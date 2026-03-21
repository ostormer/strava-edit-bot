using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StravaEditBotApi.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path
        );

        // ProblemDetails is the standard RFC 9457 error format —
        // the .NET equivalent of FastAPI's HTTPException detail body
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            // Only expose details in development
            Detail = env.IsDevelopment() ? exception.Message : null,
            Instance = httpContext.Request.Path,
        };

        // Set status and content type before writing the body.
        // WriteAsJsonAsync would override ContentType to "application/json",
        // so we serialize manually to keep the RFC 9457 media type.
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(problemDetails),
            cancellationToken);

        // Return true = "I handled this exception, don't fall through"
        // Return false = "I can't handle this one, let the next handler try"
        return true;
    }
}
