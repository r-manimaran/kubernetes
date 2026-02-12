using Microsoft.EntityFrameworkCore;
using TaskManagementApi.DTOs;

namespace TaskManagementApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/users", async (ApplicationDbContext db) =>
        {
            var users = await db.Users
                        .Include(u => u.Tasks)
                        .Select(u => new UserResponseDto(
                            u.Id,
                            u.UserName,
                            u.Email,
                            u.CreatedAt,
                            u.Tasks.Count))
                        .ToListAsync();

            return Results.Ok(users);
        });


        app.MapGet("/users/{id}", async (int id, ApplicationDbContext db) =>
        {
            var user = await db.Users
                            .Include(u=>u.Tasks)
                            .Where(u => u.Id == id)
                            .Select(u => new UserResponseDto(
                                    u.Id,
                                    u.UserName,
                                    u.Email,
                                    u.CreatedAt,
                                    u.Tasks.Count))
                            .FirstOrDefaultAsync();

            return user is not null ? Results.Ok(user) : Results.NotFound();
        });
        app.MapGet("/users/{id}/tasks", async (int id, ApplicationDbContext db) =>
        {
            var user = await db.Users
                            .Include(u => u.Tasks)
                            .FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
                return Results.NotFound();

            var tasks = await db.Tasks
                            .Include(t => t.User)
                            .Where(t => t.UserId == id)
                            .Select(t => new TaskResponseDto(
                                    t.Id,
                                    t.Title,
                                    t.Description,
                                    t.IsCompleted,
                                    t.CreatedAt,
                                    t.DueDate,
                                    t.CompletedAt,
                                    t.UserId,
                                    t.User.UserName))
                            .ToListAsync();

            return Results.Ok(tasks);
        });
    }
}
