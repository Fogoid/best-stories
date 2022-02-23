using BestStories.Model;

namespace BestStories.Services
{
    public interface IBestStoriesService
    {
        Task<IEnumerable<StoryDTO>> GetBestStoriesAsync();
    }
}
