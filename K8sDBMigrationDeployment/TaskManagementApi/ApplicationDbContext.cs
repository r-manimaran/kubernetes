using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Models;

namespace TaskManagementApi;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // TaskItem Configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(t => t.Description)
                .HasMaxLength(1000);
            entity.HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    public void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, UserName = "john_doe", Email = "john@example.com", CreatedAt = new DateTime(2024, 1, 1) },
            new User { Id = 2, UserName = "jane_smith", Email = "jane@example.com", CreatedAt = new DateTime(2024, 1, 2) }
            );

        // Seed Tasks
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem
            {
                Id = 1,
                Title = "Setup Kubernetes cluster",
                Description = "Configure K8s for the new application",
                IsCompleted = true,
                UserId = 1,
                CreatedAt = new DateTime(2024, 1, 2),
                CompletedAt = new DateTime(2024, 1, 5)
            },
            new TaskItem
            {
                Id = 2,
                Title = "Implement EF Core migrations",
                Description = "Add migration job to deployment pipeline",
                IsCompleted = false,
                UserId = 1,
                CreatedAt = new DateTime(2024, 1, 6)
            },
            new TaskItem
            {
                Id = 3,
                Title = "Write API documentation",
                Description = "Document all API endpoints",
                IsCompleted = false,
                UserId = 2,
                CreatedAt = new DateTime(2024, 1, 3)
            }
        );
    }
}
