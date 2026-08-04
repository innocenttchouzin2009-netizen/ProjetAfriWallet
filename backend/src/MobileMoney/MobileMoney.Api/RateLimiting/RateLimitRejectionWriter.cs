using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.RateLimiting;

public static class RateLimitRejectionWriter
{
    public static async Task WriteAsync(HttpContext context, string policyName, int retryAfterSeconds)
    {
        var correlationId = CorrelationContext.FromHttpContext(context)?.CorrelationId ?? CorrelationIdValidator.DefaultCorrelationId;
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.Response.Headers["X-RateLimit-Policy"] = policyName;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            code = "RATE_LIMIT_EXCEEDED",
            policy = policyName,
            retryAfterSeconds,
            correlationId
        }));
    }
}
