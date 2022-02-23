namespace BestStories.Configs
{
    public class BestStoriesProperties
    {
        public string BestStoriesUri { get; }
        public string BaseItemUri { get; }
        public long Validity { get; }

        public BestStoriesProperties(IConfiguration configuration)
        {
            BestStoriesUri = configuration.GetSection("StoryProperties").GetSection("bestStoriesUri").Value;
            BaseItemUri = configuration.GetSection("StoryProperties").GetSection("baseItemUri").Value;
            Validity = long.Parse(configuration.GetSection("StoryProperties").GetSection("validity").Value);
        }

    }
}
