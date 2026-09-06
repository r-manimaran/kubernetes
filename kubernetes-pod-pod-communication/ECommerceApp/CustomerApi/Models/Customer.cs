namespace CustomerApi.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string Address { get; set; }

    public string City { get; set; }

    public string State { get; set; }
    public string Email { get; set; }

    public string phone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
