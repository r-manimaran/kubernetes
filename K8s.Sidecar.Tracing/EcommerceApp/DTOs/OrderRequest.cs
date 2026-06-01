using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.DTOs;

public class OrderRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be a positive integer.")]
    public int CustomerId { get; set; }
    [Required, MaxLength(100)]
    public string CustomerName { get; set; } =string.Empty;
    [Required, MinLength(1, ErrorMessage = "At least one item required")]
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    [Required, MaxLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;
}

public class  OrderItem
{
    [Required]
    public int ProductId { get; set; }
    [Range(1,1000)]
    public int Quantity { get; set; }
    [Range(0.01,100000)]
    public decimal Price { get; set; }
}