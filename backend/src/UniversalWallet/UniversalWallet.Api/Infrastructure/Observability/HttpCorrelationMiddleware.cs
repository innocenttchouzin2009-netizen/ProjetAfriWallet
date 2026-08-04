using System.Diagnostics;

namespace UniversalWallet.Api.Infrastructure.Observability;

public sealed class HttpCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpCorrelationMiddleware> _logger;

    public HttpCorrelationMiddleware(RequestDelegate next, ILogger<HttpCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = context.Request.Headers["x-correlation-id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        var requestId = context.Request.Headers["x-request-id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        var sessionId = context.Request.Headers["x-session-id"].FirstOrDefault();
        var awid = context.Request.Headers["x-awid"].FirstOrDefault();
        var walletId = context.Request.Query["walletId"].FirstOrDefault();

        context.Items["TraceId"] = traceId;
        context.Items["CorrelationId"] = correlationId;
        context.Items["RequestId"] = requestId;
        context.Items["SessionId"] = sessionId;
        context.Items["AWID"] = awid;
        context.Items["WalletId"] = walletId;

        context.Response.Headers["x-correlation-id"] = correlationId;
        context.Response.Headers["x-request-id"] = requestId;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            _logger.LogInformation(
                "Handled request traceId={TraceId} correlationId={CorrelationId} requestId={RequestId} sessionId={SessionId} awid={Awid} walletId={WalletId} method={Method} path={Path} statusCode={StatusCode} durationMs={DurationMs} result={Result}",
                traceId, correlationId, requestId, sessionId, awid, walletId, context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, "Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed traceId={TraceId} correlationId={CorrelationId} requestId={RequestId} sessionId={SessionId} awid={Awid} walletId={WalletId} method={Method} path={Path} durationMs={DurationMs} result={Result}",
                traceId, correlationId, requestId, sessionId, awid, walletId, context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds, "Failed");
            throw;
        }
    }
}
