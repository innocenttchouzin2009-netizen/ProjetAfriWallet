using AfriWallet.Merchants.Settlement.Api.Contracts;
using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Application.Commands;
using AfriWallet.Merchants.Settlement.Application.Policies;
using AfriWallet.Merchants.Settlement.Application.Services;
using AfriWallet.Merchants.Settlement.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("MerchantSettlementDatabase") ??
    Environment.GetEnvironmentVariable("AFW_MERCHANT_SETTLEMENT_DB_CONNECTION_STRING") ??
    "Data Source=merchant-settlement.db";

builder.Services.AddDbContext<MerchantSettlementDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IMerchantCaptureSnapshotStore, EfMerchantCaptureSnapshotStore>();
builder.Services.AddScoped<ICaptureEligibleDecisionReader, DurableCaptureEligibleDecisionReader>();
builder.Services.AddScoped<IMerchantSettlementRepository, EfMerchantSettlementRepository>();
builder.Services.AddScoped<IMerchantSettlementAuditStore, EfMerchantSettlementAuditStore>();
builder.Services.AddSingleton<IMerchantSettlementProvider, SandboxMerchantSettlementProvider>();
builder.Services.AddSingleton<IMerchantSettlementClock, SystemMerchantSettlementClock>();
builder.Services.AddSingleton<MerchantSettlementRoutingPolicy>();
builder.Services.AddSingleton<MerchantSettlementRetryPolicy>();
builder.Services.AddScoped<MerchantSettlementService>();

var app = builder.Build();
const string Actor = "afriwallet-merchant-settlement-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-BE-MERCHANT-SETTLEMENT-1",
    durableCaptureHandoff = true,
    durableOrchestration = true,
    durableAudit = true,
    realCapturePerformed = false,
    realSettlementPerformed = false,
    realPayoutPerformed = false,
    merchantFundsMoved = false,
    walletBalanceMutated = false,
    directLedgerMutationPerformed = false
}));

app.MapPost("/internal/merchant-captures", async (
    RecordMerchantCaptureRequest request,
    IMerchantCaptureSnapshotStore captures,
    CancellationToken ct) =>
{
    if (request.DecisionId == Guid.Empty || request.PaymentIntentId == Guid.Empty ||
        string.IsNullOrWhiteSpace(request.MerchantId) ||
        !string.Equals(request.DecisionType, "CaptureEligible", StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(request.DecisionStatus, "Approved", StringComparison.OrdinalIgnoreCase) ||
        request.AmountMinor <= 0 ||
        string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        return Results.BadRequest(new { code = "MERCHANT_CAPTURE_INVALID" });

    var snapshot = new CaptureEligibleDecisionSnapshot(
        request.DecisionId,
        request.PaymentIntentId,
        request.MerchantId.Trim(),
        "CaptureEligible",
        "Approved",
        request.AmountMinor,
        request.Currency.Trim().ToUpperInvariant(),
        request.MerchantRegistryStatus,
        request.MerchantVerificationStatus);

    await captures.SaveAsync(snapshot, ct);
    return Results.Created($"/internal/merchant-captures/{snapshot.DecisionId:D}", snapshot);
});

app.MapPost("/api/v1/merchant-settlements", async (
    CreateMerchantSettlementRequest request,
    MerchantSettlementService service,
    CancellationToken ct) =>
{
    var result = await service.CreateAsync(
        new CreateMerchantSettlementCommand(request.PaymentDecisionId, request.Route, request.IdempotencyKey, Actor),
        ct);
    return Results.Created($"/api/v1/merchant-settlements/{result.SettlementId:D}", result);
});

app.MapPost("/api/v1/merchant-settlements/{id:guid}/dispatch", async (
    Guid id, MerchantSettlementService service, CancellationToken ct) =>
    Results.Ok(await service.DispatchAsync(new DispatchMerchantSettlementCommand(id, Actor), ct)));

app.MapPost("/api/v1/merchant-settlements/{id:guid}/retry", async (
    Guid id, MerchantSettlementService service, CancellationToken ct) =>
    Results.Ok(await service.RetryAsync(new RetryMerchantSettlementCommand(id, Actor), ct)));

app.MapPost("/api/v1/merchant-settlements/{id:guid}/compensate", async (
    Guid id, MerchantSettlementService service, CancellationToken ct) =>
    Results.Ok(await service.CompensateAsync(new CompensateMerchantSettlementCommand(id, Actor), ct)));

app.MapPost("/api/v1/merchant-settlements/{id:guid}/complete", async (
    Guid id, MerchantSettlementService service, CancellationToken ct) =>
    Results.Ok(await service.CompleteAsync(new CompleteMerchantSettlementCommand(id, Actor), ct)));

app.MapGet("/api/v1/merchant-settlements/{id:guid}", async (
    Guid id, MerchantSettlementService service, CancellationToken ct) =>
    Results.Ok(await service.GetAsync(id, ct)));

app.Run();
