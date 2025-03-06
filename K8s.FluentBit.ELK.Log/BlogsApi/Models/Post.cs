namespace BlogsApi.Models;

public class Post : Base
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime PublishedOn { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
}
