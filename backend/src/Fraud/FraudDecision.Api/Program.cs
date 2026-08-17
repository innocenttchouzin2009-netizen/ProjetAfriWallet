using AfriWallet.Fraud.Decision.Api.Contracts;
using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Application.Policies;
using AfriWallet.Fraud.Decision.Application.Services;
using AfriWallet.Fraud.Decision.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDeviceRiskDecisionReader, SandboxDeviceRiskDecisionReader>();
builder.Services.AddSingleton<ITransactionFraudDecisionReader, SandboxTransactionFraudDecisionReader>();
builder.Services.AddSingleton<IFraudDecisionRepository, InMemoryFraudDecisionRepository>();
builder.Services.AddSingleton<IFraudDecisionAuditStore, InMemoryFraudDecisionAuditStore>();
builder.Services.AddSingleton<IFraudDecisionClock, SystemFraudDecisionClock>();
builder.Services.AddSingleton<FraudDecisionPolicy>();
builder.Services.AddSingleton<FraudDecisionService>();

var app = builder.Build();
const string actor = "afriwallet-fraud-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0017.4",
    execution = "DECISION ONLY"
}));

app.MapPost("/api/v1/fraud/decisions/evaluate", async (
    EvaluateFraudDecisionRequest request,
    FraudDecisionService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.EvaluateAsync(
        new EvaluateFraudDecisionCommand(request.TransactionId, request.Awid, request.DeviceId, actor),
        cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/v1/fraud/decisions/by-transaction/{transactionId:guid}", async (
    Guid transactionId,
    IFraudDecisionRepository repository,
    CancellationToken cancellationToken) =>
{
    var result = await repository.GetByTransactionAsync(transactionId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.Run();