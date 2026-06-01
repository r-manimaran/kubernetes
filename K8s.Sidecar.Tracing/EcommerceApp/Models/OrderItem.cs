namespace EcommerceApp.Models;

public class OrderItem
{
    public Guid OrderItemId { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}