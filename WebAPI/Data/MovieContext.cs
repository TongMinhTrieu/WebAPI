using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Data
{
    public class MovieContext:DbContext
    {
        public MovieContext(DbContextOptions<MovieContext> options) : base(options) 
        {

        }
        public DbSet<Movie> Movies { get; set; } = null!;

        public DbSet<User> Users { get; set; }

    }
}
