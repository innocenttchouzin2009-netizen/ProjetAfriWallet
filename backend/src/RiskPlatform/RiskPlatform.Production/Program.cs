using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
    var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
    var queueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit", 0);

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "risk-fixed",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Correlation-Id"] = correlationId;
    }

    context.Response.Headers["X-Correlation-Id"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";

    if (!context.Request.IsHttps && !app.Environment.IsDevelopment())
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "HTTPS required outside development" });
        return;
    }

    await next();
});

static bool RequireInternalAccess(HttpContext context, IConfiguration configuration)
{
    var configuredKey = Environment.GetEnvironmentVariable("AFW_RISK_INTERNAL_API_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey))
    {
        configuredKey = configuration.GetValue<string>("RiskProduction:InternalApiKeyFallback", "internal-dev-key");
    }

    var provided = context.Request.Headers["X-Internal-Key"].FirstOrDefault();
    return !string.IsNullOrWhiteSpace(provided) && string.Equals(provided, configuredKey, StringComparison.Ordinal);
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", dependencies = "ok" }));
app.MapGet("/health/startup", (IConfiguration configuration) =>
{
    var valid = configuration.GetSection("OpenTelemetry").Exists() && configuration.GetSection("Resilience").Exists();
    return valid
        ? Results.Ok(new { status = "startup-ok", configuration = "valid" })
        : Results.Problem("Startup configuration invalid", statusCode: 500);
});

app.MapGet("/metrics", (HttpContext context, IConfiguration configuration) =>
{
    if (!configuration.GetValue<bool>("FeatureFlags:EnableMetricsEndpoint", false))
    {
        return Results.NotFound();
    }

    if (!RequireInternalAccess(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        service = "risk-platform",
        counters = new Dictionary<string, int>
        {
            ["risk_decisions_total"] = 0,
            ["risk_alerts_total"] = 0,
            ["audit_events_total"] = 0
        }
    });
});

app.MapGet("/internal/audit/events", (HttpContext context, IConfiguration configuration) =>
{
    if (!RequireInternalAccess(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        events = new[]
        {
            new { eventType = "RISK_EVALUATION", actor = "system", correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault(), details = "masked" }
        }
    });
}).RequireRateLimiting("risk-fixed");

app.MapGet("/internal/telemetry/status", (HttpContext context, IConfiguration configuration) =>
{
    if (!RequireInternalAccess(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        openTelemetryEnabled = configuration.GetValue<bool>("OpenTelemetry:Enabled"),
        endpoint = configuration.GetValue<string>("OpenTelemetry:Endpoint"),
        serviceName = configuration.GetValue<string>("OpenTelemetry:ServiceName")
    });
});

app.MapGet("/api/v1/risk/readiness", () => Results.Ok(new { platform = "risk", status = "ready" }))
    .RequireRateLimiting("risk-fixed");

app.Run();
