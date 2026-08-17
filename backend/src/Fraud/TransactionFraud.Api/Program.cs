using AfriWallet.Fraud.TransactionFraud.Api.Contracts;
using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Application.Policies;
using AfriWallet.Fraud.TransactionFraud.Application.Services;
using AfriWallet.Fraud.TransactionFraud.Domain.Transactions;
using AfriWallet.Fraud.TransactionFraud.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SandboxFraudSignalReader>();
builder.Services.AddSingleton<IFraudSignalReader>(sp => sp.GetRequiredService<SandboxFraudSignalReader>());
builder.Services.AddSingleton<SandboxDeviceRiskReader>();
builder.Services.AddSingleton<IDeviceRiskReader>(sp => sp.GetRequiredService<SandboxDeviceRiskReader>());
builder.Services.AddSingleton<ITransactionFraudRepository, InMemoryTransactionFraudRepository>();
builder.Services.AddSingleton<ITransactionFraudAuditStore, InMemoryTransactionFraudAuditStore>();
builder.Services.AddSingleton<ITransactionFraudClock, SystemTransactionFraudClock>();
builder.Services.AddSingleton<TransactionFraudPolicy>();
builder.Services.AddSingleton<TransactionFraudDetectionService>();

var app = builder.Build();
const string Actor = "afriwallet-fraud-system";

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", delivery = "AFW-DLV-0017.3", execution = "NON_BLOCKING" }));

app.MapPost("/api/v1/fraud/transactions/detect", async (
    DetectTransactionFraudRequest request,
    TransactionFraudDetectionService service,
    CancellationToken ct) =>
{
    var transaction = new FraudTransaction(
        request.TransactionId,
        request.Awid,
        request.DeviceId,
        request.BeneficiaryId,
        request.AmountMinor,
        request.CurrencyCode,
        request.CountryCode,
        request.OccurredAtUtc);

    var result = await service.DetectAsync(new DetectTransactionFraudCommand(transaction, Actor), ct);
    return Results.Ok(result);
});

app.MapGet("/api/v1/fraud/transactions/{transactionId:guid}/detection", async (
    Guid transactionId,
    ITransactionFraudRepository repository,
    CancellationToken ct) =>
{
    var result = await repository.GetByTransactionAsync(transactionId, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.Run();

public partial class Program;
