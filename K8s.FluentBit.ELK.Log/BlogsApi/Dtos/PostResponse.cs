﻿namespace BlogsApi.Dtos;

public class PostResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string Category { get; set; }
}
