using AfriWallet.Disputes.Resolution.Api.Contracts;
using AfriWallet.Disputes.Resolution.Application.Abstractions;
using AfriWallet.Disputes.Resolution.Application.Commands;
using AfriWallet.Disputes.Resolution.Application.Policies;
using AfriWallet.Disputes.Resolution.Application.Services;
using AfriWallet.Disputes.Resolution.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IResolutionRepository, InMemoryResolutionRepository>();
builder.Services.AddSingleton<IDisputeDecisionReader, SandboxDisputeDecisionReader>();
builder.Services.AddSingleton<IResolutionProvider, SandboxResolutionProvider>();
builder.Services.AddSingleton<IResolutionAuditStore, InMemoryResolutionAuditStore>();
builder.Services.AddSingleton<IResolutionClock, SystemResolutionClock>();
builder.Services.AddSingleton<ResolutionRoutingPolicy>();
builder.Services.AddSingleton<ResolutionRetryPolicy>();
builder.Services.AddSingleton<ResolutionOrchestrationService>();

var app = builder.Build();
const string Actor = "afriwallet-resolution-orchestration-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.5",
    scope = "SANDBOX RESOLUTION ORCHESTRATION ONLY",
    realRefund = false,
    realChargeback = false,
    moneyMovement = false,
    directLedgerMutation = false,
    externalProviderSettlement = false
}));

app.MapPost("/api/v1/disputes/resolutions", async (
    CreateResolutionRequest request,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
{
    var result = await service.CreateAsync(new CreateResolutionCommand(request.DecisionId, request.IdempotencyKey, Actor), ct);
    return Results.Created($"/api/v1/disputes/resolutions/{result.ResolutionId}", result);
});

app.MapPost("/api/v1/disputes/resolutions/{id:guid}/dispatch", async (
    Guid id,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
        Results.Ok(await service.DispatchAsync(new DispatchResolutionCommand(id, Actor), ct)));

app.MapPost("/api/v1/disputes/resolutions/{id:guid}/retry", async (
    Guid id,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
        Results.Ok(await service.RetryAsync(new RetryResolutionCommand(id, Actor), ct)));

app.MapPost("/api/v1/disputes/resolutions/{id:guid}/compensate", async (
    Guid id,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
        Results.Ok(await service.CompensateAsync(new CompensateResolutionCommand(id, Actor), ct)));

app.MapPost("/api/v1/disputes/resolutions/{id:guid}/resolve", async (
    Guid id,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
        Results.Ok(await service.ResolveAsync(new ResolveResolutionCommand(id, Actor), ct)));

app.MapGet("/api/v1/disputes/resolutions/{id:guid}", async (
    Guid id,
    ResolutionOrchestrationService service,
    CancellationToken ct) =>
        Results.Ok(await service.GetAsync(id, ct)));

app.Run();
