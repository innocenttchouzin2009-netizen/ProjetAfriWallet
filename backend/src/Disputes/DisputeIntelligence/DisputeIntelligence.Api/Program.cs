using AfriWallet.Disputes.Intelligence.Api.Contracts;
using AfriWallet.Disputes.Intelligence.Application.Abstractions;
using AfriWallet.Disputes.Intelligence.Application.Policies;
using AfriWallet.Disputes.Intelligence.Application.Services;
using AfriWallet.Disputes.Intelligence.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDisputeIntelligenceSource, SandboxDisputeIntelligenceSource>();
builder.Services.AddSingleton<IDisputeIntelligenceRepository, InMemoryDisputeIntelligenceRepository>();
builder.Services.AddSingleton<IDisputeIntelligenceAuditStore, InMemoryDisputeIntelligenceAuditStore>();
builder.Services.AddSingleton<IDisputeIntelligenceClock, SystemDisputeIntelligenceClock>();
builder.Services.AddSingleton<CustomerProtectionPolicy>();
builder.Services.AddSingleton<CustomerProtectionService>();

var app = builder.Build();
const string Actor = "afriwallet-dispute-intelligence-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0018.6",
    deterministic = true,
    explainable = true,
    automaticMerchantBlocking = false,
    automaticCustomerSuspension = false,
    refundExecution = false,
    moneyMovement = false,
    ledgerMutation = false
}));

app.MapPost("/api/v1/disputes/intelligence/evaluate", async (
    EvaluateProtectionRequest request,
    CustomerProtectionService service,
    CancellationToken ct) =>
{
    var result = await service.EvaluateAsync(new EvaluateProtectionCommand(request.SubjectId, Actor), ct);
    return Results.Ok(result);
});

app.MapGet("/api/v1/disputes/intelligence/{subjectId}", async (
    string subjectId,
    IDisputeIntelligenceRepository repository,
    CancellationToken ct) =>
{
    var result = await repository.GetLatestAsync(subjectId, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.Run();
