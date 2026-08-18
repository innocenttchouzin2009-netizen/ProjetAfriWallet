using AfriWallet.Disputes.Registry.Api.Contracts;
using AfriWallet.Disputes.Registry.Application.Abstractions;
using AfriWallet.Disputes.Registry.Application.Claims;
using AfriWallet.Disputes.Registry.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDisputeClaimRepository, InMemoryDisputeClaimRepository>();
builder.Services.AddSingleton<IDisputeRegistryAuditStore, InMemoryDisputeRegistryAuditStore>();
builder.Services.AddSingleton<IDisputeRegistryClock, SystemDisputeRegistryClock>();
builder.Services.AddSingleton<DisputeRegistryService>();

var app = builder.Build();
const string actor = "afriwallet-dispute-registry";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.1",
    scope = "CLAIM REGISTRY ONLY",
    refundDecision = false,
    chargeback = false,
    moneyMovement = false
}));

app.MapPost("/api/v1/disputes/claims", async (RegisterDisputeClaimRequest request, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.RegisterAsync(new RegisterDisputeClaimCommand(
        request.Awid,
        request.TransactionId,
        request.Type,
        request.Reason,
        request.AmountMinor,
        request.Currency,
        request.Description,
        request.SourceChannel,
        request.PaymentReference,
        request.BankTransferReference,
        request.MerchantReference,
        actor), ct)));

app.MapGet("/api/v1/disputes/claims/{claimId:guid}", async (Guid claimId, IDisputeClaimRepository repository, CancellationToken ct) =>
    (await repository.GetAsync(claimId, ct)) is { } claim ? Results.Ok(claim) : Results.NotFound());

app.MapGet("/api/v1/disputes/claims/by-awid/{awid}", async (string awid, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.GetByAwidAsync(awid, ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/submit", async (Guid claimId, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.SubmitAsync(claimId, actor, ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/open", async (Guid claimId, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.OpenAsync(claimId, actor, ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/review", async (Guid claimId, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.StartReviewAsync(claimId, actor, ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/evidence", async (Guid claimId, LinkDisputeEvidenceRequest request, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.LinkEvidenceAsync(new LinkDisputeEvidenceCommand(claimId, request.Type, request.ReferenceId, request.Summary, actor), ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/resolve", async (Guid claimId, ResolveDisputeClaimRequest request, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.ResolveAsync(new ResolveDisputeClaimCommand(claimId, request.Outcome, actor), ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/close", async (Guid claimId, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.CloseAsync(claimId, actor, ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/reject", async (Guid claimId, RejectDisputeClaimRequest request, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.RejectAsync(new RejectDisputeClaimCommand(claimId, request.Reason, actor), ct)));

app.MapPost("/api/v1/disputes/claims/{claimId:guid}/cancel", async (Guid claimId, CancelDisputeClaimRequest request, DisputeRegistryService service, CancellationToken ct) =>
    Results.Ok(await service.CancelAsync(new CancelDisputeClaimCommand(claimId, request.Reason, actor), ct)));

app.Run();
