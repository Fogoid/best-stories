using BestStories.Configs;
using BestStories.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BestStories.Services
{
    public class BestStoriesService : IBestStoriesService
    {
        private DateTime lastUpdate;
        
        private readonly BestStoriesProperties properties;
        private readonly ILogger<BestStoriesService> logger;
        private readonly Dictionary<DateTime, IList<Story>> bestStoriesDic = new();

        public BestStoriesService(ILogger<BestStoriesService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.properties = new BestStoriesProperties(configuration);
        }

        public async Task<IEnumerable<Story>> GetBestStoriesAsync()
        {
            // Update stories only if they are past validity time
            if (lastUpdate.AddMilliseconds(properties.Validity).CompareTo(DateTime.UtcNow) == -1)
            {
                await UpdateStoriesAsync();
            }

            return bestStoriesDic[lastUpdate];
        }

        private Story ConvertToStoryDTO(Story story)
        {
            return new Story()
            {
                Title = story.Title,
                Uri = story.Uri,
                PostedBy = story.PostedBy,
                Time = story.Time,
                Score = story.Score,
                CommentCount = story.CommentCount
            };
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
            var bestInstantStories = new List<Story>();


            var result = await response.Content.ReadAsStringAsync();
            var bestStories = JArray.Parse(result);

            var i = 0;
            var totalChecked = 0;
            while (i < 20 && totalChecked < bestStories.Count)
            {
                var story = bestStories[i];

                var itemUri = properties.BaseItemUri + story.ToString() + ".json";
                var itemResponse = await client.GetAsync(itemUri);

                if (itemResponse.IsSuccessStatusCode)
                {
                    var itemResult = await itemResponse.Content.ReadAsStringAsync();
                    JObject itemDetails = JsonConvert.DeserializeObject<JObject>(itemResult);

                    var itemStory = CreateStory(itemDetails);

                    bestInstantStories.Add(itemStory);

                    i++;
                }
                totalChecked++;

            }

            if (bestInstantStories.Count != 20)
            {
                //Throw exception
            }

            lastUpdate = newTime;
            bestStoriesDic.Add(newTime, bestInstantStories);
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
