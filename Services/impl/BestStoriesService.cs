using BestStories.Configs;
using BestStories.Exceptions;
using BestStories.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Timers;

namespace BestStories.Services
{
    public class BestStoriesService : IBestStoriesService
    {
        private DateTime lastUpdate;
        private List<Task> activeTasks;
        private int storiesCounter;

        private readonly BestStoriesProperties properties;
        private readonly ILogger<BestStoriesService> logger;
        private readonly Dictionary<DateTime, IList<Story>> bestStoriesDic = new();

        public BestStoriesService(ILogger<BestStoriesService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.properties = new BestStoriesProperties(configuration);
            ScheduleBestStoriesUpdate();
        }

        private void ScheduleBestStoriesUpdate()
        {
            var timer = new System.Timers.Timer(properties.Validity);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            logger.LogInformation(e.SignalTime + ": Started update on best stories");
            
            var oldLastUpdate = lastUpdate;
            
            await UpdateStoriesAsync();
            
            if (oldLastUpdate == lastUpdate)
            {
                logger.LogError(e.SignalTime + ": Update was not performed successfully");
                return;
            }

            logger.LogInformation(e.SignalTime + ": Update performed successfully");
        }

        public async Task<IEnumerable<Story>> GetBestStoriesAsync()
        {
            if (lastUpdate.AddMilliseconds(properties.Validity).CompareTo(DateTime.UtcNow) > -1)
            {
                return bestStoriesDic[lastUpdate];
            }

            throw new NoValidTimesException("Could not retrieve best stories. Try again later");
        }

        private async Task UpdateStoriesAsync()
        {
            var client = new HttpClient();
            var response = await client.GetAsync(properties.BestStoriesUri);

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var newTime = DateTime.UtcNow;

            var result = await response.Content.ReadAsStringAsync();
            var bestStories = JArray.Parse(result);

            activeTasks = new List<Task>();
            storiesCounter = 20;
            var currentBestStories = new ConcurrentBag<Story>();

            for (var i = 0; i < 20; i++)
            {
                var j = i;
                var task = Task.Run(async () => {
                    Story? s = await GetBestStoryAsync(j, bestStories);
                    if (s != null)
                    {
                        currentBestStories.Add(s);
                    }
                });

                activeTasks.Add(task);
            }

            await Task.WhenAll(activeTasks);

            if (currentBestStories.Count != 20)
            {
                return;
            }

            lastUpdate = newTime;
            var storiesToAdd = new List<Story>(currentBestStories);
            storiesToAdd.Sort((s, s2) => - s.Score.CompareTo(s2.Score));
            bestStoriesDic.Add(newTime, storiesToAdd);
        }

        private async Task<Story?> GetBestStoryAsync(int index, JArray bestStories)
        {
            var client = new HttpClient();
            var story = bestStories[index];

            var itemUri = properties.BaseItemUri + story.ToString() + ".json";
            var itemResponse = await client.GetAsync(itemUri);

            if (itemResponse.IsSuccessStatusCode)
            {
                var itemResult = await itemResponse.Content.ReadAsStringAsync();
                JObject itemDetails = JsonConvert.DeserializeObject<JObject>(itemResult);


                return CreateStory(itemDetails);
    
            }

            return null;
        }

        private Story CreateStory(JObject itemDetails)
        {
            Story story = new Story
            {
                Title = ParseStringFromJson(itemDetails, "title"),
                Uri = ParseStringFromJson(itemDetails, "url"),
                PostedBy = ParseStringFromJson(itemDetails, "by"),
                Time = ParseDateTimeFromJson(itemDetails, "time"),
                Score = ParseIntFromJson(itemDetails, "score"),
                CommentCount = ParseIntFromJson(itemDetails, "descendants")
            };

            return story;
        }

        private string? ParseStringFromJson(JObject jobject, string key)
        {
            if (jobject.ContainsKey(key))
            {
                return jobject[key].ToString();
            }

            logger.LogWarning($"Required property {key} is not in the story");
            return null;
        }

        private int ParseIntFromJson(JObject jobject, string key)
        {
            if (jobject.ContainsKey(key))
            {
                return (int)jobject[key];
            }

            logger.LogWarning($"Story does not contain required property: {key}");
            return 0;
        }

        private DateTime? ParseDateTimeFromJson(JObject jobject, string key)
        {
            if (jobject.ContainsKey(key))
            {
                var unixTime = (double)jobject[key];

                var beggining = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return beggining.AddSeconds(unixTime).ToUniversalTime();
            }

            logger.LogWarning($"Story does not contain required property: {key}");
            return null;
        }
    }
}
