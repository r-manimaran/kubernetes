namespace BlogsApi.Models;

public class Category :Base
{
    public Category()
    {
        Posts = new HashSet<Post>();
    }
    public string Name { get; set; }

    public ICollection<Post> Posts { get; set; }
}
