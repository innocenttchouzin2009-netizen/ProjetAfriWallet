using AfriWallet.BankingPlatform.BankTransferIntent.Application;
using AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;
using AfriWallet.BankingPlatform.BankTransferIntent.Application.Services;
using AfriWallet.BankingPlatform.BankTransferIntent.Infrastructure.Repositories;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IBankTransferIntentRepository,
    InMemoryBankTransferIntentRepository>();

builder.Services.AddSingleton<
    IBeneficiaryRegistryGateway,
    SandboxBeneficiaryRegistryGateway>();

builder.Services.AddScoped<
    BankTransferIntentService>();

builder.Services.AddOpenApi();

var app =
    builder.Build();

app.MapGet(
    "/health/live",
    () => Results.Ok(new
    {
        service =
            "afriwallet-bank-transfer-intent",
        status = "healthy"
    }));

app.MapPost(
    "/api/v1/banking/transfer-intents",
    async (
        CreateBankTransferIntentRequest request,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        var transfer =
            await service.CreateAsync(
                request,
                ct);

        return Results.Created(
            $"/api/v1/banking/transfer-intents/{transfer.TransferIntentId}",
            transfer);
    });

app.MapGet(
    "/api/v1/banking/transfer-intents/{transferIntentId:guid}",
    async (
        Guid transferIntentId,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        var transfer =
            await service.GetAsync(
                transferIntentId,
                ct);

        return transfer is null
            ? Results.NotFound()
            : Results.Ok(transfer);
    });

app.MapGet(
    "/api/v1/banking/owners/{ownerAwid}/transfer-intents",
    async (
        string ownerAwid,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        return Results.Ok(
            await service.ListByOwnerAsync(
                ownerAwid,
                ct));
    });

app.MapPost(
    "/api/v1/banking/transfer-intents/{transferIntentId:guid}/confirm",
    async (
        Guid transferIntentId,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        return Results.Ok(
            await service.ConfirmAsync(
                transferIntentId,
                ct));
    });

app.MapPost(
    "/api/v1/banking/transfer-intents/{transferIntentId:guid}/ready",
    async (
        Guid transferIntentId,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        return Results.Ok(
            await service.MarkReadyForRoutingAsync(
                transferIntentId,
                ct));
    });

app.MapPost(
    "/api/v1/banking/transfer-intents/{transferIntentId:guid}/cancel",
    async (
        Guid transferIntentId,
        BankTransferIntentService service,
        CancellationToken ct) =>
    {
        return Results.Ok(
            await service.CancelAsync(
                transferIntentId,
                ct));
    });

app.MapOpenApi();

app.Run();

public partial class Program;
