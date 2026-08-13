using AfriWallet.BankingPlatform.BankSettlement.Application;
using AfriWallet.BankingPlatform.BankSettlement.Application.Services;
using AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Gateways;
using AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBankSettlementRepository, InMemoryBankSettlementRepository>();
builder.Services.AddSingleton<IReconciliationRepository, InMemoryReconciliationRepository>();
builder.Services.AddSingleton<IBankExecutionGateway, SandboxBankExecutionGateway>();

builder.Services.AddScoped<BankSettlementService>();
builder.Services.AddScoped<BankReconciliationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { service = "afriwallet-bank-settlement", status = "healthy" }));

app.MapPost("/api/v1/banking/settlement-batches",
    async (CreateSettlementBatchRequest request, BankSettlementService service, CancellationToken ct) =>
    {
        var batch = await service.CreateBatchAsync(request, ct);
        return Results.Accepted($"/api/v1/banking/settlement-batches/{batch.SettlementBatchId}", batch);
    });

app.MapPost("/api/v1/banking/settlement-batches/{settlementBatchId:guid}/items",
    async (Guid settlementBatchId, AddSettlementItemRequest request, BankSettlementService service, CancellationToken ct) =>
    {
        var batch = await service.AddItemAsync(settlementBatchId, request, ct);
        return Results.Ok(batch);
    });

app.MapPost("/api/v1/banking/settlement-batches/{settlementBatchId:guid}/close",
    async (Guid settlementBatchId, BankSettlementService service, CancellationToken ct) =>
    {
        return Results.Ok(await service.CloseBatchAsync(settlementBatchId, ct));
    });

app.MapGet("/api/v1/banking/settlement-batches/{settlementBatchId:guid}",
    async (Guid settlementBatchId, BankSettlementService service, CancellationToken ct) =>
    {
        var batch = await service.GetOpenBatchesAsync(ct);
        var match = batch.FirstOrDefault(x => x.SettlementBatchId == settlementBatchId);
        return match is null ? Results.NotFound() : Results.Ok(match);
    });

app.MapPost("/api/v1/banking/reconciliation",
    async (ReconciliationRequest request, BankReconciliationService service, CancellationToken ct) =>
    {
        return Results.Ok(await service.ReconcileAsync(request, ct));
    });

app.MapGet("/api/v1/banking/reconciliation/{settlementBatchId:guid}",
    async (Guid settlementBatchId, BankReconciliationService service, CancellationToken ct) =>
    {
        var records = await service.GetForBatchAsync(settlementBatchId, ct);
        return Results.Ok(records);
    });

app.MapOpenApi();

app.Run();

public partial class Program;
