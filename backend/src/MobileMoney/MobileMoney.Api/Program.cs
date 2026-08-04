using MobileMoney.Production.Audit;
using MobileMoney.Production.Correlation;
using MobileMoney.Production.Diagnostics;
using MobileMoney.Production.Errors;
using MobileMoney.Production.Extensions;
using MobileMoney.Production.Health;
using MobileMoney.Production.Logging;
using MobileMoney.Production.RateLimiting;
using MobileMoney.Production.FeatureFlags;
using MobileMoney.Production.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMtnMomoProductionConfiguration(builder.Configuration);
builder.Services.AddSingleton<StructuredOperationLogger>();

if (builder.Environment.IsEnvironment("Staging") || builder.Environment.IsEnvironment("Production"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}
else
{
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MobileMoneyExceptionMiddleware>();
app.UseMiddleware<FeatureGateMiddleware>();
app.UseRateLimiter();

app.MapGet("/health/live", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/health/ready", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector" or "mtn-momo-readiness", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/health/startup", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/internal/configuration/diagnostics", (ConfigurationDiagnosticService diagnostics, FeatureFlagDiagnosticService featureDiagnostics) =>
{
    return Results.Ok(new
    {
        configuration = diagnostics.GetDiagnosticSnapshot(),
        featureFlags = featureDiagnostics.GetDiagnosticSnapshot()
    });
});

app.MapGet("/internal/mobile-money/telemetry/diagnostics", (TelemetryDiagnosticService telemetryDiagnostics) =>
{
    return Results.Ok(telemetryDiagnostics.GetDiagnosticSnapshot());
});

app.MapGet("/metrics", () => Results.Ok());

app.MapGet("/internal/logging/echo", (HttpContext httpContext, StructuredOperationLogger logger) =>
{
    var correlation = CorrelationContext.FromHttpContext(httpContext) ?? new CorrelationContext(CorrelationIdValidator.Generate());
    using var scope = logger.BeginScope(new MobileMoneyLoggingScope(correlation));
    logger.LogRequestAccepted(MobileMoneyLogEvents.RequestAccepted, correlation, 15000, "XAF", "237670000000");
    return Results.Ok(new { correlationId = correlation.CorrelationId });
});

app.MapPost("/mtn-momo/status", async context =>
{
    await context.Response.WriteAsJsonAsync(new { status = "ok" });
}).RequireRateLimiting(MobileMoneyRateLimitPolicies.StatusPerIp).AllowAnonymous();

app.MapPost("/mtn-momo/deposit", async context =>
{
    await context.Response.WriteAsJsonAsync(new { status = "accepted" });
}).RequireRateLimiting(MobileMoneyRateLimitPolicies.OperationsPerAwid).AllowAnonymous();

app.MapPost("/mtn-momo/callback", async context =>
{
    await context.Response.WriteAsJsonAsync(new { status = "received" });
}).RequireRateLimiting(MobileMoneyRateLimitPolicies.Callback).AllowAnonymous();

app.MapPost("/internal/audit/records", (IAuditService auditService, AuditRecord record) =>
{
    var saved = auditService.Record(record);
    return Results.Ok(saved);
});

app.MapGet("/internal/audit/records", (IAuditService auditService, string? auditId, string? transactionId, string? correlationId, string? providerReference, string? walletId, DateTime? from, DateTime? to, AuditAction? action, AuditResult? result) =>
{
    var criteria = new AuditSearchCriteria
    {
        AuditId = auditId,
        TransactionId = transactionId,
        CorrelationId = correlationId,
        ProviderReference = providerReference,
        WalletId = walletId,
        From = from,
        To = to,
        Action = action,
        Result = result
    };

    return Results.Ok(auditService.Search(criteria));
});

app.MapGet("/internal/audit/records/{auditId}/verify", (IAuditService auditService, string auditId) =>
{
    var isValid = auditService.VerifyChain(auditId);
    return Results.Ok(new { auditId, isValid });
});

app.MapGet("/internal/audit/export", (IAuditService auditService, AuditExportService exportService, DateTime? from, DateTime? to, string? providerCode, AuditResult? result, string? operationType, string format = "json") =>
{
    var filter = new AuditExportFilter
    {
        From = from,
        To = to,
        ProviderCode = providerCode,
        Result = result,
        OperationType = operationType
    };

    var records = auditService.Export(filter).ToList();
    return format.Equals("csv", StringComparison.OrdinalIgnoreCase)
        ? Results.Text(exportService.ExportCsv(records), "text/csv")
        : Results.Ok(records);
});

app.Run();
