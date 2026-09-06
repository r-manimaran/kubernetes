using Bogus;
using CustomerApi.Models;

namespace CustomerApi.Endpoints;

public static class CustomerEndpoints
{
    private static readonly List<Customer> _customers = new Faker<Customer>()
        .RuleFor(c => c.Id, f => f.IndexFaker + 1)
        .RuleFor(c => c.Name, f => f.Name.FullName())
        .RuleFor(c => c.Address, f => f.Address.StreetAddress())
        .RuleFor(c => c.City, f => f.Address.City())
        .RuleFor(c => c.State, f => f.Address.State())
        .RuleFor(c => c.Email, f => f.Internet.Email())
        .RuleFor(c => c.phone, f => f.Phone.PhoneNumber())
        .Generate(50);

    public static void MapCustomerEndpoints(this WebApplication app)
    {
        app.MapGet("/customers", GetCustomers);
    }

    private static IResult GetCustomers(int page = 1, int pageSize = 10)
    {
        var items = _customers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Results.Ok(new
        {
            page,
            pageSize,
            totalCount = _customers.Count,
            totalPages = (int)Math.Ceiling(_customers.Count / (double)pageSize),
            data = items
        });
    }
}
