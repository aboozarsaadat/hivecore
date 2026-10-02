using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HiveCore.Middleware;

/// <summary>
/// Logs each incoming HTTP request with method, path, status, elapsed time,
/// user agent, and (if authenticated) user id.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly HashSet<string> _excludedPaths;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Paths that we don't want to log (static assets, health checks)
        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/favicon.ico",
            "/robots.txt"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip logging for excluded paths and static files
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        var method = context.Request.Method;
        var queryString = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value
            : string.Empty;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        context.Items["RequestStartTimeUtc"] = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var level = statusCode >= 500 ? LogLevel.Error
                : statusCode >= 400 ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "HTTP {Method} {Path}{Query} => {StatusCode} in {ElapsedMs}ms | User={UserId} | IP={Ip} | ReqId={RequestId} | UA={UserAgent}",
                method,
                path,
                queryString,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                userId ?? "anonymous",
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                requestId,
                Truncate(userAgent, 120));

            // Add Server-Timing header for performance monitoring
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Response-Time-ms"] =
                    stopwatch.ElapsedMilliseconds.ToString();
            }
        }
    }

    private bool ShouldSkip(string path)
    {
        if (_excludedPaths.Contains(path)) return true;

        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string Truncate(string value, int maxLength)
        => string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength] + "...";
}