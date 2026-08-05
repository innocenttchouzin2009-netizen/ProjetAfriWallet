using AfriWallet.Merchant.Application.Services;
using MerchantDomain = AfriWallet.Merchant.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MerchantRegistryService>();
builder.Services.AddSingleton<QrPaymentService>();
builder.Services.AddSingleton<SettlementService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/api/v1/merchants", async (MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchants = await service.GetAllAsync(cancellationToken);
    return Results.Ok(merchants);
});

app.MapGet("/api/v1/merchants/{merchantId}", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.GetByIdAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants", async (MerchantDomain.Merchant merchant, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(merchant, cancellationToken);
        return Results.Created($"/api/v1/merchants/{created.MerchantId}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPut("/api/v1/merchants/{merchantId}", async (string merchantId, MerchantDomain.Merchant merchant, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateAsync(merchantId, merchant, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapPost("/api/v1/merchants/{merchantId}/activate", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.ActivateAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants/{merchantId}/suspend", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.SuspendAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants/{merchantId}/close", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.CloseAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapGet("/api/v1/qr-payments", async (QrPaymentService service, CancellationToken cancellationToken) =>
{
    var payments = await service.GetAllAsync(cancellationToken);
    return Results.Ok(payments);
});

app.MapPost("/api/v1/qr-payments", async (MerchantDomain.QrPayment payment, QrPaymentService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(payment, cancellationToken);
    return Results.Created($"/api/v1/qr-payments/{created.PaymentId}", created);
});

app.MapGet("/api/v1/settlements", async (SettlementService service, CancellationToken cancellationToken) =>
{
    var settlements = await service.GetAllAsync(cancellationToken);
    return Results.Ok(settlements);
});

app.MapPost("/api/v1/settlements", async (MerchantDomain.MerchantSettlement settlement, SettlementService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(settlement, cancellationToken);
    return Results.Created($"/api/v1/settlements/{created.SettlementId}", created);
});

app.Run();
