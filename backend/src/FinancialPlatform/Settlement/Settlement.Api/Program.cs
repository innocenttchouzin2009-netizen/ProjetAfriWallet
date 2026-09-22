using AfriWallet.Balance.Application;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Persistence;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.Interfaces;
using Settlement.Application.Services;
using Settlement.Contracts.Requests;
using Settlement.Infrastructure.Gateways;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Providers;
using Settlement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var settlementConnectionString =
    builder.Configuration.GetConnectionString("SettlementDatabase") ??
    Environment.GetEnvironmentVariable("AFW_SETTLEMENT_DB_CONNECTION_STRING") ??
    "Data Source=settlement.db";

var ledgerConnectionString =
    builder.Configuration.GetConnectionString("LedgerDatabase") ??
    Environment.GetEnvironmentVariable("AFW_LEDGER_DB_CONNECTION_STRING") ??
    "Data Source=universal-ledger.db";

builder.Services.AddDbContext<SettlementDbContext>(options => options.UseSqlite(settlementConnectionString));
builder.Services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(ledgerConnectionString));

builder.Services.AddScoped<ISettlementRepository, EfSettlementRepository>();
builder.Services.AddScoped<IJournalRepository, EfJournalRepository>();
builder.Services.AddScoped<LedgerPostingApplicationService>();
builder.Services.AddScoped<ILedgerJournalReader, EfLedgerJournalReader>();
builder.Services.AddScoped<BalanceProjectionService>();
builder.Services.AddScoped<LedgerBackedBalanceReadService>();
builder.Services.AddSingleton(TimeProvider.System);

var fxRates = new[]
{
    new ConfiguredFxRate("EUR", "XAF", 655.957m),
    new ConfiguredFxRate("USD", "XAF", 600m),
    new ConfiguredFxRate("XAF", "EUR", 1m / 655.957m),
    new ConfiguredFxRate("XAF", "USD", 1m / 600m)
};
builder.Services.AddSingleton<AfriWallet.Fx.Application.IFxQuoteProvider>(sp =>
    new ConfiguredFxQuoteProvider(fxRates, sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<FxQuoteApplicationService>();
builder.Services.AddScoped<Settlement.Application.Interfaces.IFxQuoteProvider, CoreFxQuoteProviderAdapter>();
var eurXafSourceClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:EUR_XAF:SourceAccountId"] ?? "10000000-0000-0000-0000-000000000001");
var eurXafDestinationClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:EUR_XAF:DestinationAccountId"] ?? "10000000-0000-0000-0000-000000000002");
var usdXafSourceClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:USD_XAF:SourceAccountId"] ?? "10000000-0000-0000-0000-000000000003");
var usdXafDestinationClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:USD_XAF:DestinationAccountId"] ?? "10000000-0000-0000-0000-000000000004");
var xafEurSourceClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:XAF_EUR:SourceAccountId"] ?? "10000000-0000-0000-0000-000000000005");
var xafEurDestinationClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:XAF_EUR:DestinationAccountId"] ?? "10000000-0000-0000-0000-000000000006");
var xafUsdSourceClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:XAF_USD:SourceAccountId"] ?? "10000000-0000-0000-0000-000000000007");
var xafUsdDestinationClearing = Guid.Parse(builder.Configuration["Settlement:FxClearing:XAF_USD:DestinationAccountId"] ?? "10000000-0000-0000-0000-000000000008");

builder.Services.AddSingleton<ISettlementFxClearingAccountResolver>(
    new ConfiguredSettlementFxClearingAccountResolver(
        [
            new("EUR", eurXafSourceClearing, "XAF", eurXafDestinationClearing),
            new("USD", usdXafSourceClearing, "XAF", usdXafDestinationClearing),
            new("XAF", xafEurSourceClearing, "EUR", xafEurDestinationClearing),
            new("XAF", xafUsdSourceClearing, "USD", xafUsdDestinationClearing)
        ]));
builder.Services.AddScoped<ITreasurySettlementGateway, LedgerBackedTreasurySettlementGateway>();
builder.Services.AddScoped<MultiCurrencySettlementService>();
builder.Services.AddScoped<SettlementPositionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SettlementDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<LedgerDbContext>().Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "Settlement.Api",
    delivery = "AFW-BE-SETTLEMENT-1",
    durablePersistence = true,
    ledgerBackedSameCurrency = true,
    crossCurrencyLedgerPosting = true
}));

app.MapPost(
    "/api/v1/settlement/instructions",
    async (CreateSettlementInstructionRequest request, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
    {
        var instruction = await service.CreateInstructionAsync(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.SourceCurrency,
            request.DestinationCurrency,
            request.SourceAmountMinor,
            cancellationToken);

        return Results.Created($"/api/v1/settlement/instructions/{instruction.InstructionId}", instruction);
    });

app.MapPost(
    "/api/v1/settlement/instructions/{instructionId:guid}/execute",
    async (Guid instructionId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
    {
        try
        {
            var instruction = await service.ExecuteInstructionAsync(instructionId, cancellationToken);
            return Results.Ok(instruction);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("FX clearing accounts", StringComparison.Ordinal))
        {
            return Results.Conflict(new { code = "SETTLEMENT_FX_CLEARING_NOT_CONFIGURED", message = exception.Message });
        }
    });

app.MapGet(
    "/api/v1/settlement/instructions/{instructionId:guid}",
    async (Guid instructionId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
    {
        var instruction = await service.GetInstructionAsync(instructionId, cancellationToken);
        return instruction is null ? Results.NotFound() : Results.Ok(instruction);
    });

app.MapPost(
    "/api/v1/settlement/batches",
    async (CreateSettlementBatchRequest request, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
    {
        var batch = await service.CreateBatchAsync(request.InstructionIds, cancellationToken);
        return Results.Created($"/api/v1/settlement/batches/{batch.BatchId}", batch);
    });

app.MapPost(
    "/api/v1/settlement/batches/{batchId:guid}/execute",
    async (Guid batchId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
    {
        var batch = await service.ExecuteBatchAsync(batchId, cancellationToken);
        return Results.Ok(batch);
    });

app.MapGet(
    "/api/v1/settlement/positions",
    async (SettlementPositionService service, CancellationToken cancellationToken) =>
    {
        var positions = await service.GetPositionsAsync(cancellationToken);
        return Results.Ok(positions);
    });

app.MapGet(
    "/api/v1/settlement/quotes",
    async (string from, string to, long amountMinor, Settlement.Application.Interfaces.IFxQuoteProvider quoteProvider, CancellationToken cancellationToken) =>
    {
        var quote = await quoteProvider.GetQuoteAsync(from, to, amountMinor, cancellationToken);
        return Results.Ok(quote);
    });

app.Run();
