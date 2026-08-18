using AfriWallet.Disputes.Decision.Api.Contracts;
using AfriWallet.Disputes.Decision.Application.Abstractions;
using AfriWallet.Disputes.Decision.Application.Commands;
using AfriWallet.Disputes.Decision.Application.Services;
using AfriWallet.Disputes.Decision.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDisputeDecisionRepository, InMemoryDisputeDecisionRepository>();
builder.Services.AddSingleton<IInvestigationOutcomeReader, SandboxInvestigationOutcomeReader>();
builder.Services.AddSingleton<IDisputeDecisionAuditStore, InMemoryDisputeDecisionAuditStore>();
builder.Services.AddSingleton<IDisputeDecisionClock, SystemDisputeDecisionClock>();
builder.Services.AddSingleton<DisputeDecisionPolicy>();
builder.Services.AddSingleton<DisputeDecisionService>();

var app = builder.Build();
const string Actor = "afriwallet-dispute-decision-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.4",
    scope = "REFUND AND CHARGEBACK DECISION ONLY",
    refundExecution = false,
    chargebackExecution = false,
    moneyMovement = false,
    ledgerMutation = false
}));

app.MapPost("/api/v1/disputes/decisions/evaluate", async (
    EvaluateDecisionRequest request,
    DisputeDecisionService service,
    CancellationToken ct) =>
    Results.Ok(await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(request.InvestigationId, Actor), ct)));

app.MapPost("/api/v1/disputes/decisions/{id:guid}/approve", async (
    Guid id,
    ApproveDecisionRequest request,
    DisputeDecisionService service,
    CancellationToken ct) =>
    Results.Ok(await service.ApproveAsync(new ApproveDisputeDecisionCommand(id, request.Approver, request.Note, Actor), ct)));

app.MapPost("/api/v1/disputes/decisions/reevaluate", async (
    ReevaluateDecisionRequest request,
    DisputeDecisionService service,
    CancellationToken ct) =>
{
    return Results.Ok(
        await service.ReevaluateAsync(
            new ReevaluateDisputeDecisionCommand(
                request.InvestigationId,
                Actor,
                request.Reason),
            ct));
});

app.MapGet("/api/v1/disputes/decisions/{id:guid}", async (
    Guid id,
    DisputeDecisionService service,
    CancellationToken ct) =>
{
    return Results.Ok(
        await service.GetAsync(
            id,
            ct));
});

app.Run();
