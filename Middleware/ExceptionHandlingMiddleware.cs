using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HiveCore.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches unhandled exceptions, logs them, and returns a proper JSON error response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} — {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled exception on {Method} {Path} — {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write error response.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow,
            stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to perform this action."),

            ArgumentNullException argNull => (
                (int)HttpStatusCode.BadRequest,
                "Invalid argument",
                argNull.ParamName != null
                    ? $"The parameter '{argNull.ParamName}' is required."
                    : "A required argument was null."),

            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Invalid argument",
                argEx.Message),

            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Resource not found",
                "The requested resource could not be found."),

            InvalidOperationException invOp => (
                (int)HttpStatusCode.BadRequest,
                "Invalid operation",
                invOp.Message),

            NotImplementedException => (
                (int)HttpStatusCode.NotImplemented,
                "Not implemented",
                "This feature is not yet implemented."),

            TimeoutException => (
                (int)HttpStatusCode.GatewayTimeout,
                "Request timed out",
                "The operation took too long to complete."),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal server error",
                "An unexpected error occurred. Please try again later.")
        };
    }
}