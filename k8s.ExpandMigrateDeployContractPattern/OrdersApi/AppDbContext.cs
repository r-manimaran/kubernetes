using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;

namespace OrdersApi;

public class AppDbContext :DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) :base(options)
    {
        
    }

    public DbSet<Order> Orders => Set<Order>();
}
