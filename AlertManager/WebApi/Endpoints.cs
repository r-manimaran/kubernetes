namespace WebApi;

public static class Endpoints
{
    private static readonly List<Product> _products = new()
    {
            new Product { Id = 1, Name = "Laptop", Price = 1200 },
            new Product { Id = 2, Name = "Headphones", Price = 150 },
            new Product { Id = 3, Name = "Mouse", Price = 50 }
    };

    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Hello World!");

        app.MapGet("/products", () => _products).WithName("GetProducts");

        app.MapGet("/products/{id:int}", (int id) =>
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        }).WithName("GetProductById");

        app.MapPost("/products", (Product product) =>
        {
            product.Id = _products.Max(p => p.Id) + 1;
            _products.Add(product);
            return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        }).WithName("CreateProduct");

        app.MapPut("/products/{id:int}", (int id, Product updatedProduct) =>
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product is null)
            {
                return Results.NotFound();
            }
            product.Name = updatedProduct.Name;
            product.Price = updatedProduct.Price;
            return Results.NoContent();
        }).WithName("UpdateProduct");

        app.MapDelete("/products/{id:int}", (int id) =>
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product is null)
            {
                return Results.NotFound();
            }
            _products.Remove(product);
            return Results.NoContent();
        }).WithName("DeleteProduct");

    }
}
