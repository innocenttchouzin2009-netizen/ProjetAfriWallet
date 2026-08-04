using Microsoft.AspNetCore.Http;

namespace MobileMoney.Production.Correlation;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incomingCorrelationId = context.Request.Headers["X-Correlation-ID"].ToString();
        var transactionId = context.Request.Headers["X-Transaction-ID"].ToString();
        var providerReference = context.Request.Headers["X-Provider-Reference"].ToString();
        var awidId = context.Request.Headers["X-Awid-ID"].ToString();
        var walletId = context.Request.Headers["X-Wallet-ID"].ToString();
        var providerCode = context.Request.Headers["X-Provider-Code"].ToString();
        var operationType = context.Request.Headers["X-Operation-Type"].ToString();

        var correlationId = CorrelationIdValidator.CreateOrGenerate(incomingCorrelationId);
        var correlationContext = new CorrelationContext(
            correlationId,
            string.IsNullOrWhiteSpace(transactionId) ? null : transactionId,
            string.IsNullOrWhiteSpace(providerReference) ? null : providerReference,
            string.IsNullOrWhiteSpace(awidId) ? null : awidId,
            string.IsNullOrWhiteSpace(walletId) ? null : walletId,
            string.IsNullOrWhiteSpace(providerCode) ? null : providerCode,
            string.IsNullOrWhiteSpace(operationType) ? null : operationType);

        context.Items[CorrelationContext.ItemKey] = correlationContext;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Response.Headers["X-Transaction-ID"] = correlationContext.TransactionId ?? string.Empty;
        context.Response.Headers["X-Provider-Reference"] = correlationContext.ProviderReference ?? string.Empty;
        context.Response.Headers["X-Awid-ID"] = correlationContext.AwidId ?? string.Empty;
        context.Response.Headers["X-Wallet-ID"] = correlationContext.WalletId ?? string.Empty;
        context.Response.Headers["X-Provider-Code"] = correlationContext.ProviderCode ?? string.Empty;
        context.Response.Headers["X-Operation-Type"] = correlationContext.OperationType ?? string.Empty;

        await _next(context);
    }
}
