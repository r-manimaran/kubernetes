using BlogsApi.Data;
using BlogsApi.Models;
using Bogus;

namespace BlogsApi;

public class DataSeeder
{
    private readonly AppDbContext _context;

    public DataSeeder(AppDbContext context)
    {
        _context = context;
    }
    public void SeedData()
    {
        // Category Faker
        var categoyFaker = new Faker<Category>()
            .RuleFor(c => c.Name, f => f.Commerce.Categories(1)[0]);

        // product Faker
        var postFaker = new Faker<Post>()
            .RuleFor(p => p.Title, f => f.Lorem.Text())
            .RuleFor(p => p.Content, f => f.Lorem.Text())
            .RuleFor(p => p.PublishedOn, f => f.Date.Past(10))
            .RuleFor(p => p.Category, f => categoyFaker.Generate());

        // Generate 50 categories
        var categories = categoyFaker.Generate(50);
        _context.Categories.AddRange(categories);
        Console.WriteLine($"Generated {categories.Count} categories and inserted");
        //Generate 10000 products in total, 200 products in each category
        foreach (var category in categories)
        {
            var posts = postFaker.RuleFor(p => p.Category, category).Generate(200);
            _context.Posts.AddRange(posts);
            Console.WriteLine($"Category {category.Name} has {posts.Count} products");
        }

        _context.SaveChanges();
        Console.WriteLine("Data seeded successfully");

    }
}
