using EcommerceApp.DTOs;
using EcommerceApp.Models;

namespace EcommerceApp.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(OrderRequest request, string correlationId,CancellationToken cancellationToken=default);
    Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken=default);
}

public sealed class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;
    // In a real application, you would inject repositories or database contexts here
    //private readonly AppDbContext _db;
    //private readonly IMessagePublisher _messagePublisher;
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    public async Task<Order> CreateOrderAsync(OrderRequest request, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating order for Customer {CustomerId}. CorrelationId: {CorrelationId}",
            request.CustomerId, correlationId);

        // Simulate order creation logic
        var order = new Order
        {
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            ShippingAddress = request.ShippingAddress,
            Items = request.Items.Select(i => new Models.OrderItem
            {
                ProductId = i.ProductId,
                ProductName = $"Product {i.ProductId}",
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            CorrelationId = correlationId,
            CreatedBy = "System"
        };
        // await _db.Orders.AddAsync(order, ct);
        // await _db.SaveChangesAsync(ct);
        // await _publisher.PublishAsync("order.created", order, ct);

        _logger.LogInformation(
            "Order {OrderId} created. Total: {Total}. CorrelationId: {CorrelationId}",
            order.OrderId, order.TotalAmount, correlationId);

        return order;
    }
    public async Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Simulate fetching order logic
        var order = new Order
        {
            OrderId = orderId,
            CustomerId = 123,
            CustomerName = "John Doe",
            ShippingAddress = "123 Main St",
            Items = new List<Models.OrderItem>
            {
                new Models.OrderItem { ProductId = 1, ProductName = "Product 1", Price = 10.0m, Quantity = 2 },
                new Models.OrderItem { ProductId = 2, ProductName = "Product 2", Price = 20.0m, Quantity = 1 }
            },
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedBy = "System"
        };
        _logger.LogInformation("Fetched order with CorrelationId: {CorrelationId}", order.CorrelationId);
        return await Task.FromResult(order);
    }
}
