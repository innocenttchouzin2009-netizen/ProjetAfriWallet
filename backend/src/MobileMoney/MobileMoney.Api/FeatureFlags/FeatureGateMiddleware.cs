using Microsoft.AspNetCore.Http;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.FeatureFlags;

public sealed class FeatureGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMobileMoneyFeatureManager _featureManager;

    public FeatureGateMiddleware(RequestDelegate next, IMobileMoneyFeatureManager featureManager)
    {
        _next = next;
        _featureManager = featureManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var feature = context.Request.Headers["X-Feature-Flag"].ToString();
        if (string.IsNullOrWhiteSpace(feature))
        {
            await _next(context);
            return;
        }

        if (!_featureManager.IsFeatureAllowed(feature))
        {
            var correlationId = CorrelationContext.FromHttpContext(context)?.CorrelationId ?? CorrelationIdValidator.DefaultCorrelationId;
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers["X-Feature-Flag"] = feature;
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { code = "FEATURE_DISABLED", feature, correlationId });
            return;
        }

        await _next(context);
    }
}
