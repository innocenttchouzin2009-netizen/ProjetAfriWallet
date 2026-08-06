using System.Diagnostics;

namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantCorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        context.Items["MerchantCorrelationContext"] = new MerchantCorrelationContext(correlationId, traceId);
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Response.Headers["X-Trace-ID"] = traceId;
        await next(context);
    }
}

public sealed record MerchantCorrelationContext(string? CorrelationId, string TraceId);
