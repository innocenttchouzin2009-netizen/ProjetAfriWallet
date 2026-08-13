using AfriWallet.BankingPlatform.BankTransferExecution.Application;
using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;
using AfriWallet.BankingPlatform.BankTransferExecution.Application.Services;
using AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Gateways;
using AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Repositories;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IBankTransferExecutionRepository,
    InMemoryBankTransferExecutionRepository>();

builder.Services.AddSingleton<
    ITransferIntentGateway,
    SandboxTransferIntentGateway>();

builder.Services.AddSingleton<
    IBankRoutingGateway,
    SandboxBankRoutingGateway>();

builder.Services.AddSingleton<
    IBankProviderGateway,
    SandboxBankProviderGateway>();

builder.Services.AddScoped<
    BankTransferExecutionService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet(
    "/health/live",
    () => Results.Ok(new
    {
        service =
            "afriwallet-bank-transfer-execution",
        status = "healthy"
    }));

app.MapPost(
    "/api/v1/banking/transfer-executions",
    async (
        ExecuteBankTransferRequest request,
        BankTransferExecutionService service,
        CancellationToken ct) =>
    {
        var execution =
            await service.ExecuteAsync(
                request,
                ct);

        return Results.Accepted(
            $"/api/v1/banking/transfer-executions/{execution.ExecutionId}",
            execution);
    });

app.MapGet(
    "/api/v1/banking/transfer-executions/{executionId:guid}",
    async (
        Guid executionId,
        BankTransferExecutionService service,
        CancellationToken ct) =>
    {
        var execution =
            await service.GetAsync(
                executionId,
                ct);

        return execution is null
            ? Results.NotFound()
            : Results.Ok(execution);
    });

app.MapPost(
    "/api/v1/banking/transfer-executions/{executionId:guid}/complete",
    async (
        Guid executionId,
        BankTransferExecutionService service,
        CancellationToken ct) =>
    {
        return Results.Ok(
            await service.CompleteAsync(
                executionId,
                ct));
    });

app.MapOpenApi();

app.Run();

public partial class Program;
