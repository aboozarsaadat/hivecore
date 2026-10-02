using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiveCore.Filters;

/// <summary>
/// Attribute that protects API endpoints with a static API key.
/// Key must be supplied in the header "X-Api-Key".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private const string ConfigKey = "Api:Key";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices
            .GetService(typeof(IConfiguration)) as IConfiguration;

        var logger = context.HttpContext.RequestServices
            .GetService(typeof(ILogger<ApiKeyAuthAttribute>)) as ILogger<ApiKeyAuthAttribute>;

        var expectedKey = config?[ConfigKey];

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            logger?.LogError("API key is not configured (configuration key '{Key}').", ConfigKey);

            context.Result = new ObjectResult(new
            {
                error = "Server configuration error",
                status = 500
            })
            { StatusCode = 500 };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey))
        {
            logger?.LogWarning("Request to {Path} is missing API key.", context.HttpContext.Request.Path);

            context.Result = new UnauthorizedObjectResult(new
            {
                error = "API key is missing",
                status = 401
            });
            return;
        }

        if (!string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            logger?.LogWarning(
                "Request to {Path} provided an invalid API key.",
                context.HttpContext.Request.Path);

            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Invalid API key",
                status = 401
            });
            return;
        }

        await next();
    }
}