namespace OrdersApi.Models;

public sealed class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsPriority { get; set; }
}
