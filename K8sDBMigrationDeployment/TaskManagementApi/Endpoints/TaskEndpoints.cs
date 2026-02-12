using Microsoft.EntityFrameworkCore;
using TaskManagementApi.DTOs;
using TaskManagementApi.Models;

namespace TaskManagementApi.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        app.MapGet("/tasks", async (ApplicationDbContext db, ILogger<Program> logger) =>
        {
           var tasks = await db.Tasks.Include(t => t.User)
            .Select(t=>new TaskResponseDto(
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


        app.MapGet("/tasks/{id}", async (int id, ApplicationDbContext db) =>
        {
            var task = await db.Tasks.Include(t => t.User).Where(t => t.Id == id)
            .Select(t=>new TaskResponseDto(
                t.Id, 
                t.Title, 
                t.Description, 
                t.IsCompleted, 
                t.CreatedAt, 
                t.DueDate, 
                t.CompletedAt, 
                t.UserId, 
                t.User.UserName)).FirstOrDefaultAsync();

            return task is not null ? Results.Ok(task) : Results.NotFound();
        });


        app.MapPost("/tasks", async (CreateTaskDto task, ApplicationDbContext db) =>
        {
           var user = await db.Users.FindAsync(task.UserId);
            if (user is null) return Results.BadRequest($"User with ID {task.UserId} does not exist.");

            var newTask = new TaskItem
            {
                Title = task.Title,
                Description = task.Description,
                UserId = task.UserId
            };
            db.Tasks.Add(newTask);
            await db.SaveChangesAsync();

            var response = new TaskResponseDto(
                newTask.Id, 
                newTask.Title, 
                newTask.Description, 
                newTask.IsCompleted, 
                newTask.CreatedAt, 
                newTask.DueDate, 
                newTask.CompletedAt, 
                newTask.UserId, 
                user.UserName);
            return Results.Created($"/tasks/{newTask.Id}", response);
        });
        app.MapPut("/tasks/{id}", async (int id, UpdateTaskDto updatedTask, ApplicationDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.IsCompleted = updatedTask.IsCompleted;
            task.DueDate = updatedTask.DueDate;
            await db.SaveChangesAsync();
            return Results.Ok(task);
        });

        app.MapDelete("/tasks/{id}", async (int id, ApplicationDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
       
    }
}
