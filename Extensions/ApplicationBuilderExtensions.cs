using HiveCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HiveCore.Extensions;

/// <summary>
/// Extension methods for IApplicationBuilder to wire up HiveCore middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the HiveCore middleware pipeline (exception handling + request logging).
    /// Should be called very early in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseHiveCorePipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds Serilog request logging (skips static files & health).
    /// </summary>
    public static IApplicationBuilder UseHiveCoreRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent",
                    httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIp",
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId",
                        httpContext.User.FindFirst(
                            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                }
            };
        });

        return app;
    }

    /// <summary>
    /// Enables Swagger UI at /api/docs in non-production environments.
    /// </summary>
    public static IApplicationBuilder UseHiveCoreSwagger(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "HiveCore API v1");
                options.RoutePrefix = "api/docs";
                options.DocumentTitle = "HiveCore API Documentation";
                options.DisplayRequestDuration();
            });
        }

        return app;
    }
}