using AfriWallet.Fraud.Decision.Api.Contracts;
using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Application.Policies;
using AfriWallet.Fraud.Decision.Application.Services;
using AfriWallet.Fraud.Decision.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("FraudDecision")
    ?? "Data Source=fraud-decision.db";
builder.Services.AddDbContext<FraudDecisionDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<IDeviceRiskDecisionReader, SandboxDeviceRiskDecisionReader>();
builder.Services.AddSingleton<ITransactionFraudDecisionReader, SandboxTransactionFraudDecisionReader>();
builder.Services.AddScoped<IFraudDecisionRepository, EfFraudDecisionRepository>();
builder.Services.AddScoped<IFraudDecisionAuditStore, EfFraudDecisionAuditStore>();
builder.Services.AddSingleton<IFraudDecisionClock, SystemFraudDecisionClock>();
builder.Services.AddSingleton<FraudDecisionPolicy>();
builder.Services.AddScoped<FraudDecisionService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FraudDecisionDbContext>();
    await db.Database.EnsureCreatedAsync();
}
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