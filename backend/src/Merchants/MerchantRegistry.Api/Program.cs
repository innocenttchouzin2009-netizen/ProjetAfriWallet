using AfriWallet.Merchants.Registry.Api.Contracts;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Application.Commands;
using AfriWallet.Merchants.Registry.Application.Services;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using AfriWallet.Merchants.Registry.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var merchantRegistryConnectionString =
    builder.Configuration.GetConnectionString("MerchantRegistryDatabase") ??
    Environment.GetEnvironmentVariable("AFW_MERCHANT_REGISTRY_DB_CONNECTION_STRING") ??
    "Data Source=merchant-registry.db";

builder.Services.AddDbContext<MerchantRegistryDbContext>(options =>
    options.UseSqlite(merchantRegistryConnectionString));
builder.Services.AddScoped<IMerchantRepository, EfMerchantRepository>();
builder.Services.AddScoped<IMerchantAuditStore, EfMerchantAuditStore>();
builder.Services.AddSingleton<IMerchantClock, SystemMerchantClock>();
builder.Services.AddScoped<MerchantRegistryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MerchantRegistryDbContext>().Database.EnsureCreated();
}

const string Actor = "afriwallet-merchant-registry-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0019.1",
    scope = "MERCHANT REGISTRY AND BUSINESS PROFILE ONLY",
    kybPerformed = false,
    paymentAcceptancePerformed = false,
    paymentCapturePerformed = false,
    settlementPerformed = false,
    payoutPerformed = false,
    moneyMovementPerformed = false,
    ledgerMutationPerformed = false
}));

app.MapPost("/api/v1/merchants", async (
    CreateMerchantRequest request,
    MerchantRegistryService service,
    CancellationToken ct) =>
{
    var profile = new BusinessProfile(
        request.LegalName,
        request.TradingName,
        request.MerchantType,
        request.CountryCode,
        request.SettlementCurrency,
        request.BusinessCategory,
        request.RegistrationNumber,
        request.TaxNumber,
        new BusinessAddress(request.AddressLine1, request.AddressLine2, request.City, request.PostalCode, request.CountryCode),
        new MerchantContact(request.Email, request.Phone));

    var result = await service.CreateAsync(new CreateMerchantCommand(request.OwnerAwid, profile, Actor), ct);
    return Results.Created($"/api/v1/merchants/{result.MerchantId}", result);
});

app.MapPost("/api/v1/merchants/{merchantId}/register", async (
    string merchantId,
    MerchantRegistryService service,
    CancellationToken ct) =>
        Results.Ok(await service.RegisterAsync(new RegisterMerchantCommand(merchantId, Actor), ct)));

app.MapPut("/api/v1/merchants/{merchantId}/capabilities", async (
    string merchantId,
    SetCapabilitiesRequest request,
    MerchantRegistryService service,
    CancellationToken ct) =>
        Results.Ok(await service.SetCapabilitiesAsync(new SetMerchantCapabilitiesCommand(merchantId, request.Capabilities, Actor), ct)));

app.MapPost("/api/v1/merchants/{merchantId}/status", async (
    string merchantId,
    ChangeMerchantStatusRequest request,
    MerchantRegistryService service,
    CancellationToken ct) =>
        Results.Ok(await service.ChangeStatusAsync(new ChangeMerchantStatusCommand(merchantId, request.Status, Actor), ct)));

app.MapGet("/api/v1/merchants/{merchantId}", async (
    string merchantId,
    MerchantRegistryService service,
    CancellationToken ct) =>
        Results.Ok(await service.GetAsync(merchantId, ct)));

app.Run();
