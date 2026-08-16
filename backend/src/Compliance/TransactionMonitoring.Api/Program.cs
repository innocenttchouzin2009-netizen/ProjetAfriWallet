using AfriWallet.Compliance.TransactionMonitoring.Api.Contracts;
using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;
using AfriWallet.Compliance.TransactionMonitoring.Application.Monitoring;
using AfriWallet.Compliance.TransactionMonitoring.Application.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;
using AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITransactionHistoryRepository, InMemoryTransactionHistoryRepository>();
builder.Services.AddSingleton<IMonitoringAlertRepository, InMemoryMonitoringAlertRepository>();
builder.Services.AddSingleton<IMonitoringRuleProvider, SandboxMonitoringRuleProvider>();
builder.Services.AddSingleton<IMonitoringAuditStore, InMemoryMonitoringAuditStore>();
builder.Services.AddSingleton<IMonitoringClock, SystemMonitoringClock>();
builder.Services.AddSingleton<LargeAmountRuleEvaluator>();
builder.Services.AddSingleton<VelocityRuleEvaluator>();
builder.Services.AddSingleton<StructuringRuleEvaluator>();
builder.Services.AddSingleton<GeographicRiskRuleEvaluator>();
builder.Services.AddSingleton<RepeatedBeneficiaryRuleEvaluator>();
builder.Services.AddSingleton<TransactionMonitoringService>();

var app = builder.Build();

const string Actor = "afriwallet-system";

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy",
        delivery = "AFW-DLV-0016.4",
        monitoringPolicy = "SANDBOX"
    }));

app.MapPost(
    "/api/v1/compliance/transactions/monitor",
    async (
        MonitorTransactionRequest request,
        TransactionMonitoringService service,
        CancellationToken cancellationToken) =>
    {
        var transaction = new MonitoredTransaction(
            request.TransactionId,
            request.Awid,
            request.Direction,
            request.Channel,
            request.AmountMinor,
            request.CurrencyCode,
            request.CountryCode,
            request.CounterpartyId,
            request.BeneficiaryId,
            request.OccurredAtUtc);
        var result = await service.MonitorAsync(
            new MonitorTransactionCommand(transaction, Actor),
            cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/api/v1/compliance/alerts/by-awid/{awid}",
    async (
        string awid,
        IMonitoringAlertRepository alerts,
        CancellationToken cancellationToken) =>
            Results.Ok(await alerts.GetByAwidAsync(awid, cancellationToken)));

app.Run();

public partial class Program;