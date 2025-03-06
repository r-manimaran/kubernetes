namespace BlogsApi;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogInformation(
                "Request {Method} {Path} => {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }
}
