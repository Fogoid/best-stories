#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BestStories.Services;

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

        // GET: api/<BestStories>
        [HttpGet]
        public async Task<IEnumerable<Story>> GetAsync()
        {
            return await bestStoriesService.GetBestStoriesAsync();
        }
    }
}
