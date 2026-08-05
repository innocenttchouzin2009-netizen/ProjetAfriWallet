using AfriWallet.Merchant.Application.Services;
using MerchantDomain = AfriWallet.Merchant.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MerchantRegistryService>();
builder.Services.AddSingleton<QrPaymentService>();
builder.Services.AddSingleton<SettlementService>();
builder.Services.AddSingleton<MerchantOnboardingService>();
builder.Services.AddSingleton<MerchantOnboardingValidator>();

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

app.MapPost("/api/v1/merchant-onboarding", (string merchantId, string businessName, string legalName, string businessType, string registrationNumber, string taxIdentifier, MerchantOnboardingService service) =>
{
    var onboarding = service.StartOnboarding(merchantId, businessName, legalName, businessType, registrationNumber, taxIdentifier);
    return Results.Created($"/api/v1/merchant-onboarding/{onboarding.MerchantId}", onboarding);
});

app.MapGet("/api/v1/merchant-onboarding/{merchantId}", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.GetOnboarding(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapPut("/api/v1/merchant-onboarding/{merchantId}", (string merchantId, MerchantDomain.MerchantProfile profile, MerchantOnboardingService service) =>
{
    var onboarding = service.CompleteProfile(merchantId, profile);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapPost("/api/v1/merchant-onboarding/{merchantId}/submit", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.CreateKycCase(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapGet("/api/v1/merchant-kyc/{merchantId}", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.GetOnboarding(merchantId);
    return onboarding?.KycCase is null ? Results.NotFound() : Results.Ok(onboarding.KycCase);
});

app.MapPost("/api/v1/merchant-kyc/{merchantId}/approve", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.ApproveKyc(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapPost("/api/v1/merchant-kyc/{merchantId}/reject", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.RejectKyc(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.Run();
