using EcommerceApp.DTOs;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders");

        group.MapPost("/",CreateOrderAsync)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(201)
            .ProducesValidationProblem()
            .Produces(500);

        group.MapGet("/{orderId:guid}", GetOrderAsync)
            .WithName("GetOrder")
            .Produces<OrderResponse>(200)
            .ProducesProblem(404);            
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromBody] OrderRequest request,
        IOrderService orderService,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var correlationId = httpContext.Items["X-Correlation-ID"]?.ToString() ?? Guid.NewGuid().ToString();
        try
        {
            var order = await orderService.CreateOrderAsync(request, correlationId, ct);
            return Results.Created($"/api/v1/orders/{order.OrderId}", new OrderResponse(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order with CorrelationId: {CorrelationId}", correlationId);
            return Results.Problem("An error occurred while creating the order. Please try again later.", statusCode: 500);
        }

    }

    private static async Task<IResult> GetOrderAsync(
        Guid orderId, IOrderService orderService, CancellationToken ct)
    {
        var order = await orderService.GetOrderAsync(orderId, ct);
        return order is null 
            ? Results.Problem($"Order with ID {orderId} not found.", statusCode: 404)
            : Results.Ok(new OrderResponse(order));
    }
}

public record OrderResponse(
    Guid OrderId, string CustomerName,
    decimal TotalAmount, string Status, DateTime OrderDate)
{
    public OrderResponse(EcommerceApp.Models.Order o)
        : this(o.OrderId, o.CustomerName, o.TotalAmount, o.Status.ToString(), o.OrderDate) { }
}

