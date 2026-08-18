using AfriWallet.Disputes.Investigation.Api.Contracts;
using AfriWallet.Disputes.Investigation.Application.Abstractions;
using AfriWallet.Disputes.Investigation.Application.Commands;
using AfriWallet.Disputes.Investigation.Application.Services;
using AfriWallet.Disputes.Investigation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDisputeInvestigationRepository, InMemoryDisputeInvestigationRepository>();
builder.Services.AddSingleton<IDisputeEligibilityReader, SandboxDisputeEligibilityReader>();
builder.Services.AddSingleton<IDisputeInvestigationAuditStore, InMemoryDisputeInvestigationAuditStore>();
builder.Services.AddSingleton<IDisputeInvestigationClock, SystemDisputeInvestigationClock>();
builder.Services.AddSingleton<DisputeInvestigationService>();

var app = builder.Build();
const string actor = "afriwallet-dispute-investigation-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.3",
    scope = "EVIDENCE AND INVESTIGATION ONLY",
    refundDecision = false,
    chargeback = false,
    moneyMovement = false
}));

app.MapPost("/api/v1/disputes/investigations", async (
    CreateInvestigationRequest request,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.CreateAsync(new CreateInvestigationCommand(request.ClaimId, actor), ct)));

app.MapPost("/api/v1/disputes/investigations/{id:guid}/assign", async (
    Guid id,
    AssignInvestigationRequest request,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.AssignAsync(new AssignInvestigationCommand(id, request.AnalystId, actor), ct)));

app.MapPost("/api/v1/disputes/investigations/{id:guid}/evidence-requests", async (
    Guid id,
    RequestEvidenceRequest request,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.RequestEvidenceAsync(new RequestEvidenceCommand(id, request.Type, request.RequestedFrom, request.Reason, actor), ct)));

app.MapPost("/api/v1/disputes/investigations/{id:guid}/evidence", async (
    Guid id,
    AddEvidenceRequest request,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.AddEvidenceAsync(
        new AddEvidenceCommand(id, request.Type, request.Reference, request.Description, request.Sha256, request.SizeBytes, request.ContentType, request.SubmittedBy, actor),
        ct)));

app.MapPost("/api/v1/disputes/investigations/{id:guid}/complete", async (
    Guid id,
    CompleteInvestigationRequest request,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.CompleteAsync(new CompleteInvestigationCommand(id, request.Outcome, actor), ct)));

app.MapPost("/api/v1/disputes/investigations/{id:guid}/close", async (
    Guid id,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.CloseAsync(id, actor, ct)));

app.MapGet("/api/v1/disputes/investigations/{id:guid}", async (
    Guid id,
    DisputeInvestigationService service,
    CancellationToken ct) =>
    Results.Ok(await service.GetAsync(id, ct)));

app.Run();
