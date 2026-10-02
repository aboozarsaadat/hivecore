using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HiveCore.Middleware;

/// <summary>
/// Simple in-memory, per-IP + per-path sliding-window rate limiter.
/// Suitable for basic protection on top of AspNetCoreRateLimit.
/// For distributed deployments, prefer Redis-backed rate limiting.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Rule: (maxRequests, window)
    private static readonly Dictionary<string, (int Max, TimeSpan Window)> Rules = new()
    {
        // Path prefix → (max requests, window)
        ["/Account/Login"] = (10, TimeSpan.FromMinutes(5)),
        ["/Account/Register"] = (5, TimeSpan.FromHours(1)),
        ["/Account/ForgotPassword"] = (5, TimeSpan.FromHours(1)),
        ["/api/posts"] = (30, TimeSpan.FromMinutes(1)),
        ["/api/comments"] = (40, TimeSpan.FromMinutes(1)),
        ["/api/likes"] = (80, TimeSpan.FromMinutes(1)),
        ["/api/"] = (120, TimeSpan.FromMinutes(1))
    };

    // Bucket: key = ip + path-prefix, value = list of request timestamps
    private static readonly ConcurrentDictionary<string, List<DateTime>> Buckets = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Match the most specific rule
        var matchedPrefix = Rules.Keys
            .Where(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(prefix => prefix.Length)
            .FirstOrDefault();

        if (matchedPrefix == null)
        {
            await _next(context);
            return;
        }

        var (max, window) = Rules[matchedPrefix];
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var bucketKey = $"{ip}:{matchedPrefix}";

        var now = DateTime.UtcNow;

        var bucket = Buckets.GetOrAdd(bucketKey, _ => new List<DateTime>());

        bool allowed;
        int remaining;

        lock (bucket)
        {
            // Remove old entries outside the window
            var cutoff = now - window;
            bucket.RemoveAll(t => t < cutoff);

            if (bucket.Count >= max)
            {
                allowed = false;
                remaining = 0;
            }
            else
            {
                bucket.Add(now);
                allowed = true;
                remaining = max - bucket.Count;
            }
        }

        // Set rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = max.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Window"] = window.TotalSeconds.ToString("F0");

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP={Ip} Path={Path} (max={Max}/{Window})",
                ip, path, max, window);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var retryAfter = window.TotalSeconds.ToString("F0");
            context.Response.Headers["Retry-After"] = retryAfter;

            await context.Response.WriteAsync(
                $"{{\"error\":\"Too Many Requests\",\"message\":\"Rate limit exceeded. Please try again later.\",\"retryAfterSeconds\":{retryAfter}}}");

            return;
        }

        await _next(context);
    }
}