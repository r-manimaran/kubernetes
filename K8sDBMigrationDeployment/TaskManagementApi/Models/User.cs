namespace TaskManagementApi.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for related tasks
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
