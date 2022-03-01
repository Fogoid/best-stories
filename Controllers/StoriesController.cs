#nullable disable
using System.Net;
using BestStories.Services;
using BestStories.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BestStories.Model
{
    [Route("best20")]
    [ApiController]
    public class StoriesController : Controller
    {
        private readonly IBestStoriesService bestStoriesService;

        public StoriesController(IBestStoriesService bestStoriesService)
        {
            this.bestStoriesService = bestStoriesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                return Ok(await bestStoriesService.GetBestStoriesAsync());
            } catch (NoValidTimesException ex)
            {
                return NotFound(ex.Message);
            }
            
        }
    }
}
