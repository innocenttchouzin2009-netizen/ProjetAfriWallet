using AfriWallet.Disputes.Eligibility.Api.Contracts;
using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Application.Policies;
using AfriWallet.Disputes.Eligibility.Application.Services;
using AfriWallet.Disputes.Eligibility.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDisputeClaimReader, SandboxDisputeClaimReader>();
builder.Services.AddSingleton<ITransactionReferenceReader, SandboxTransactionReferenceReader>();
builder.Services.AddSingleton<IDisputeEligibilityRepository, InMemoryDisputeEligibilityRepository>();
builder.Services.AddSingleton<IDisputeEligibilityAuditStore, InMemoryDisputeEligibilityAuditStore>();
builder.Services.AddSingleton<IDisputeEligibilityClock, SystemDisputeEligibilityClock>();
builder.Services.AddSingleton<DisputeEligibilityPolicy>();
builder.Services.AddSingleton<DisputeClassificationPolicy>();
builder.Services.AddSingleton<DisputeEligibilityService>();

var app = builder.Build();
const string actor = "afriwallet-dispute-eligibility-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.2",
    scope = "ELIGIBILITY AND CLASSIFICATION ONLY",
    refundDecision = false,
    chargeback = false,
    moneyMovement = false
}));

app.MapPost("/api/v1/disputes/eligibility/evaluate", async (
    EvaluateDisputeEligibilityRequest request,
    DisputeEligibilityService service,
    CancellationToken ct) =>
    Results.Ok(await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(request.ClaimId, actor), ct)));

app.MapGet("/api/v1/disputes/eligibility/{claimId:guid}", async (
    Guid claimId,
    IDisputeEligibilityRepository repository,
    CancellationToken ct) =>
    (await repository.GetByClaimAsync(claimId, ct)) is { } decision ? Results.Ok(decision) : Results.NotFound());

app.Run();
