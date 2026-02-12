using System.Globalization;

namespace TaskManagementApi.DTOs;

public record CreateTaskDto(string Title, string Description, int UserId);

public record UpdateTaskDto(string Title, string Description, bool IsCompleted, DateTime? DueDate);

public record TaskResponseDto(int Id, string Title, string Description, bool IsCompleted, DateTime CreatedAt,
    DateTime? DueDate, DateTime? CompletedAt, int UserId, string Username);

public record UserResponseDto(int Id, string UserName, string Email, DateTime CreatedAt, int TaskCount);