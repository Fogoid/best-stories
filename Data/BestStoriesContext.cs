#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BestStories.Model;

namespace BestStories.Data
{
    public class BestStoriesContext : DbContext
    {
        public BestStoriesContext (DbContextOptions<BestStoriesContext> options)
            : base(options)
        {
        }

        public DbSet<BestStories.Model.Story> Story { get; set; }
    }
}
