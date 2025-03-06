namespace BlogsApi.Dtos;

public class UpdatePostRequest
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int CategoryId { get; set; }
}
