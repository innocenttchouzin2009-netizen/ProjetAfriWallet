using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Application.Services;
using MerchantSettlement.Contracts.Requests;
using MerchantSettlement.Infrastructure.Acquiring;
using MerchantSettlement.Infrastructure.FinancialCore;
using MerchantSettlement.Infrastructure.Reconciliation;
using MerchantSettlement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMerchantSettlementRepository, InMemoryMerchantSettlementRepository>();
builder.Services.AddSingleton<SandboxAcquiringReadModel>();
builder.Services.AddSingleton<IAcquiringReadModel>(sp => sp.GetRequiredService<SandboxAcquiringReadModel>());
builder.Services.AddSingleton<IFinancialSettlementGateway, SandboxFinancialSettlementGateway>();
builder.Services.AddSingleton<IFinancialReconciliationGateway, SandboxFinancialReconciliationGateway>();
builder.Services.AddScoped<MerchantSettlementPositionService>();
builder.Services.AddScoped<MerchantSettlementService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "afriwallet-merchant-settlement" }));

app.MapPost("/api/v1/merchant-settlement/profiles", async (
    CreateMerchantSettlementProfileRequest request,
    MerchantSettlementService service,
    CancellationToken cancellationToken) =>
{
    var profile = await service.CreateProfileAsync(
        request.MerchantId,
        request.SettlementCurrency,
        request.Frequency,
        request.SettlementDelayDays,
        request.MinimumSettlementMinor,
        cancellationToken);

    return Results.Created($"/api/v1/merchant-settlement/profiles/{profile.MerchantId}", profile);
});

app.MapPost("/api/v1/merchant-settlement/settlements", async (
    CreateMerchantSettlementRequest request,
    MerchantSettlementService service,
    CancellationToken cancellationToken) =>
{
    var settlement = await service.CreateSettlementAsync(
        request.MerchantId,
        request.PeriodStartUtc,
        request.PeriodEndUtc,
        request.AdjustmentsMinor,
        request.ReserveMinor,
        request.IdempotencyKey,
        cancellationToken);

    return Results.Created($"/api/v1/merchant-settlement/settlements/{settlement.SettlementId}", settlement);
});

app.MapPost("/api/v1/merchant-settlement/settlements/{settlementId:guid}/execute", async (
    Guid settlementId,
    MerchantSettlementService service,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await service.ExecuteAsync(settlementId, cancellationToken));
});

app.MapPost("/api/v1/merchant-settlement/settlements/{settlementId:guid}/reconcile", async (
    Guid settlementId,
    MerchantSettlementService service,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await service.ReconcileAsync(settlementId, cancellationToken));
});

app.MapOpenApi();
app.Run();

public partial class Program;
