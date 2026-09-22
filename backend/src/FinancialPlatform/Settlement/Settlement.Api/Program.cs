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
using Treasury.Application.Interfaces;
using Treasury.Infrastructure.Persistence;
using Treasury.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var settlementConnectionString =
    builder.Configuration.GetConnectionString("SettlementDatabase") ??
    Environment.GetEnvironmentVariable("AFW_SETTLEMENT_DB_CONNECTION_STRING") ??
    "Data Source=settlement.db";

var ledgerConnectionString =
    builder.Configuration.GetConnectionString("LedgerDatabase") ??
    Environment.GetEnvironmentVariable("AFW_LEDGER_DB_CONNECTION_STRING") ??
    "Data Source=universal-ledger.db";

var treasuryConnectionString =
    builder.Configuration.GetConnectionString("TreasuryDatabase") ??
    Environment.GetEnvironmentVariable("AFW_TREASURY_DB_CONNECTION_STRING") ??
    "Data Source=treasury.db";

builder.Services.AddDbContext<SettlementDbContext>(options => options.UseSqlite(settlementConnectionString));
builder.Services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(ledgerConnectionString));
builder.Services.AddDbContext<TreasuryDbContext>(options => options.UseSqlite(treasuryConnectionString));

builder.Services.AddScoped<ISettlementRepository, EfSettlementRepository>();
builder.Services.AddScoped<IJournalRepository, EfJournalRepository>();
builder.Services.AddScoped<ITreasuryRepository, EfTreasuryRepository>();
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
builder.Services.AddScoped<ITreasurySettlementGateway, LedgerBackedTreasurySettlementGateway>();
builder.Services.AddScoped<MultiCurrencySettlementService>();
builder.Services.AddScoped<SettlementPositionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SettlementDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<LedgerDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<TreasuryDbContext>().Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "Settlement.Api",
    delivery = "AFW-BE-SETTLEMENT-1",
    durablePersistence = true,
    ledgerBackedSameCurrency = true,
    crossCurrencyLedgerPosting = true,
    fxClearingAccountConvention = "FX-CLEARING-{CURRENCY}"
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
            exception.Message.Contains("FX clearing account", StringComparison.Ordinal))
        {
            return Results.Conflict(new { code = "SETTLEMENT_FX_CLEARING_ACCOUNT_REQUIRED", message = exception.Message });
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
