using BlogsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
    {
        
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Category> Categories { get; set; }



    
}


