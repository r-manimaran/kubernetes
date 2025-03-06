using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace BlogsApi.ExceptionHandler;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, 
                              ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                Exception exception,
                                CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            ApplicationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        Activity? activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;

        logger.LogError(exception,exception.Message);
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Type = exception.GetType().Name,
                Title = "An error occured in the Application.",
                Detail = exception.Message,
                
               // Move this to common place, if in case there are multiple ExceptionHandler
               /* Instance = httpContext.Request.Method + " " + httpContext.Request.Path,
                Extensions = new Dictionary<string, object?>()
                {
                    {"requestId", httpContext.TraceIdentifier},
                    {"traceId", activity?.Id},
                    {"spanId", activity?.SpanId.ToString()}
                }*/
            }
        });
    }
}
