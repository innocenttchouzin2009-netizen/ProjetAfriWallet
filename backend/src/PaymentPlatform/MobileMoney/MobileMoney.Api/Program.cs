using AfriWallet.PaymentPlatform.MobileMoney.Api;
using AfriWallet.PaymentPlatform.MobileMoney.Application;
using AfriWallet.PaymentPlatform.MobileMoney.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMobileMoneyProvider>(_ =>
    new SandboxMobileMoneyProvider(
        "ORANGE",
        "Orange Money",
        ["CM", "CI", "SN", "ML"],
        ["XAF", "XOF"]));

builder.Services.AddSingleton<IMobileMoneyProvider>(_ =>
    new SandboxMobileMoneyProvider(
        "MTN",
        "MTN Mobile Money",
        ["CM", "GH", "UG", "ZM"],
        ["XAF", "GHS", "UGX", "ZMW"]));

builder.Services.AddSingleton<IMobileMoneyProvider>(_ =>
    new SandboxMobileMoneyProvider(
        "AIRTEL",
        "Airtel Money",
        ["UG", "KE", "ZM"],
        ["UGX", "KES", "ZMW"]));

builder.Services.AddSingleton<IMobileMoneyProvider>(_ =>
    new SandboxMobileMoneyProvider(
        "MPESA",
        "M-Pesa",
        ["KE", "TZ"],
        ["KES", "TZS"]));

builder.Services.AddSingleton<IMobileMoneyProviderRegistry, MobileMoneyProviderRegistry>();
builder.Services.AddSingleton<MobileMoneyGateway>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "mobile-money-gateway",
    status = "healthy"
}));

app.MapGet(
    "/api/v1/mobile-money/providers",
    (IMobileMoneyProviderRegistry registry) =>
        Results.Ok(registry.GetAll().Select(provider => provider.Definition)));

app.MapPost(
    "/api/v1/mobile-money/payments",
    async (
        InitiateMobileMoneyRequest request,
        MobileMoneyGateway gateway,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await gateway.InitiateAsync(request, cancellationToken);
            return Results.Accepted($"/api/v1/mobile-money/payments/{result.Id}", result);
        }
        catch (MobileMoneyException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Code,
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/api/v1/mobile-money/payments/{id:guid}",
    (Guid id, MobileMoneyGateway gateway) =>
    {
        try
        {
            return Results.Ok(gateway.Get(id));
        }
        catch (MobileMoneyException exception)
        {
            return Results.NotFound(new
            {
                error = exception.Code,
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/api/v1/mobile-money/payments/{id:guid}/refresh",
    async (
        Guid id,
        MobileMoneyGateway gateway,
        CancellationToken cancellationToken) =>
    {
        try
        {
            return Results.Ok(await gateway.RefreshStatusAsync(id, cancellationToken));
        }
        catch (MobileMoneyException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Code,
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/api/v1/mobile-money/callbacks",
    async (
        MobileMoneyCallback callback,
        MobileMoneyGateway gateway,
        CancellationToken cancellationToken) =>
    {
        try
        {
            return Results.Ok(await gateway.ProcessCallbackAsync(callback, cancellationToken));
        }
        catch (MobileMoneyException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Code,
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/api/v1/mobile-money/operations/audit",
    (MobileMoneyGateway gateway) => Results.Ok(gateway.AuditEvents));

app.MapGet(
    "/api/v1/mobile-money/operations/telemetry",
    (MobileMoneyGateway gateway) => Results.Ok(gateway.TelemetryEvents));

app.Run();

public partial class Program;