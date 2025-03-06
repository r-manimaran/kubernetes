namespace BlogsApi.Models;

public class Base
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
