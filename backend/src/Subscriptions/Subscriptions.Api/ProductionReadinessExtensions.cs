using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;

namespace Subscriptions.Api;

public static class ProductionReadinessExtensions
{
    public static IApplicationBuilder UseProductionReadiness(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";

            if (!context.Request.IsHttps && !context.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase) && !context.Request.Host.Host.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "HTTPS required" });
                return;
            }

            context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 1024 * 1024;
            await next();
        });

        return app;
    }

    public static IEndpointConventionBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
        endpoints.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
        return endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    }
}
