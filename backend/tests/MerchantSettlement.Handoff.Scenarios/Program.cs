using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using AfriWallet.Merchants.Settlement.Infrastructure;
using AfriWallet.Merchants.Settlement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.Interfaces;
using Settlement.Application.Services;
using Settlement.Domain.Fx;
using Settlement.Domain.Instructions;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-merchant-settlement-handoff-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<MerchantSettlementDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    await using var db = new MerchantSettlementDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var repo = new EfMerchantSettlementRepository(db);
    var now = new DateTimeOffset(2026, 9, 22, 13, 0, 0, TimeSpan.Zero);

    var settlement = new MerchantSettlementOrchestration(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "merchant-001",
        MerchantSettlementRoute.MerchantSettlement,
        125_000,
        "XAF",
        "idem-merchant-001",
        now);

    await repo.AddAsync(settlement);

    var reloaded = await repo.GetAsync(settlement.SettlementId);
    Assert(reloaded is not null, "Durable merchant settlement reload failed.");
    Assert(reloaded!.SettlementId == settlement.SettlementId, "Settlement id mismatch after reload.");
    Assert(reloaded.PaymentDecisionId == settlement.PaymentDecisionId, "Decision id mismatch after reload.");
    Assert(reloaded.IdempotencyKey == settlement.IdempotencyKey, "Idempotency key mismatch after reload.");

    Assert((await repo.GetByDecisionAsync(settlement.PaymentDecisionId))?.SettlementId == settlement.SettlementId,
        "Decision lookup must resolve persisted settlement.");
    Assert((await repo.GetByIdempotencyKeyAsync(settlement.IdempotencyKey))?.SettlementId == settlement.SettlementId,
        "Idempotency lookup must resolve persisted settlement.");

    var duplicateRejected = false;
    try
    {
        await repo.AddAsync(new MerchantSettlementOrchestration(
            Guid.NewGuid(),
            settlement.PaymentDecisionId,
            Guid.NewGuid(),
            "merchant-001",
            MerchantSettlementRoute.MerchantSettlement,
            125_000,
            "XAF",
            "different-key",
            now));
    }
    catch (InvalidOperationException)
    {
        duplicateRejected = true;
    }
    Assert(duplicateRejected, "Duplicate decision must be rejected durably.");

    var source = Guid.NewGuid();
    var destination = Guid.NewGuid();
    var routes = new Dictionary<string, MerchantSettlementAccountRoute>
    {
        [ConfiguredMerchantSettlementAccountResolver.Key("merchant-001", MerchantSettlementRoute.MerchantSettlement, "XAF")] =
            new(source, destination, "XAF")
    };
    var accountResolver = new ConfiguredMerchantSettlementAccountResolver(routes);
    var settlementCore = new RecordingSettlementService();
    var provider = new MultiCurrencySettlementHandoffProvider(accountResolver, settlementCore);

    var accepted = await provider.SubmitAsync(new MerchantSettlementProviderRequest(
        settlement.SettlementId,
        settlement.PaymentDecisionId,
        settlement.PaymentIntentId,
        settlement.MerchantId,
        MerchantSettlementRoute.MerchantSettlement,
        settlement.AmountMinor,
        settlement.Currency,
        settlement.IdempotencyKey,
        "corr-001"));

    Assert(accepted.Status == MerchantSettlementProviderStatus.Accepted,
        "Merchant settlement route must hand off successfully.");
    Assert(Guid.TryParse(accepted.ProviderReference, out _),
        "Successful handoff must expose Settlement instruction id as provider reference.");
    Assert(settlementCore.CreateCalls == 1 && settlementCore.ExecuteCalls == 1,
        "Successful handoff must create and execute exactly one Settlement instruction.");
    Assert(settlementCore.LastSourceAccountId == source && settlementCore.LastDestinationAccountId == destination,
        "Configured account route must be forwarded to Settlement core.");
    Assert(settlementCore.LastAmountMinor == settlement.AmountMinor && settlementCore.LastCurrency == "XAF",
        "Amount/currency must be preserved during handoff.");

    var payout = await provider.SubmitAsync(new MerchantSettlementProviderRequest(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "merchant-001",
        MerchantSettlementRoute.MerchantPayout, 10_000, "XAF", "idem-payout", "corr-payout"));
    Assert(payout.Status == MerchantSettlementProviderStatus.PermanentFailure,
        "Merchant payout must remain outside this delivery.");

    var missingRoute = await provider.SubmitAsync(new MerchantSettlementProviderRequest(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "merchant-missing",
        MerchantSettlementRoute.MerchantSettlement, 10_000, "XAF", "idem-missing", "corr-missing"));
    Assert(missingRoute.Status == MerchantSettlementProviderStatus.PermanentFailure,
        "Missing account route must fail permanently.");

    var mismatchResolver = new ConfiguredMerchantSettlementAccountResolver(
        new Dictionary<string, MerchantSettlementAccountRoute>
        {
            [ConfiguredMerchantSettlementAccountResolver.Key("merchant-002", MerchantSettlementRoute.MerchantSettlement, "XAF")] =
                new(Guid.NewGuid(), Guid.NewGuid(), "EUR")
        });
    var mismatchProvider = new MultiCurrencySettlementHandoffProvider(mismatchResolver, settlementCore);
    var mismatch = await mismatchProvider.SubmitAsync(new MerchantSettlementProviderRequest(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "merchant-002",
        MerchantSettlementRoute.MerchantSettlement, 10_000, "XAF", "idem-mismatch", "corr-mismatch"));
    Assert(mismatch.Status == MerchantSettlementProviderStatus.PermanentFailure,
        "Configured route currency mismatch must fail permanently.");

    Assert(!await provider.CompensateAsync(accepted.ProviderReference!),
        "Automatic compensation must remain disabled in this delivery.");

    Console.WriteLine("AFW-BE-MERCHANT-SETTLEMENT-HANDOFF-1 scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class RecordingSettlementService : MultiCurrencySettlementService
{
    private readonly InMemoryRepository repository = new();

    public int CreateCalls { get; private set; }
    public int ExecuteCalls { get; private set; }
    public Guid LastSourceAccountId { get; private set; }
    public Guid LastDestinationAccountId { get; private set; }
    public long LastAmountMinor { get; private set; }
    public string? LastCurrency { get; private set; }

    public RecordingSettlementService()
        : base(new InMemoryRepository(), new SameCurrencyFxProvider(), new AcceptingTreasuryGateway())
    {
    }

    public new async Task<SettlementInstruction> CreateInstructionAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        string sourceCurrency,
        string destinationCurrency,
        long sourceAmountMinor,
        CancellationToken cancellationToken)
    {
        CreateCalls++;
        LastSourceAccountId = sourceAccountId;
        LastDestinationAccountId = destinationAccountId;
        LastAmountMinor = sourceAmountMinor;
        LastCurrency = sourceCurrency;
        return await base.CreateInstructionAsync(
            sourceAccountId, destinationAccountId, sourceCurrency, destinationCurrency, sourceAmountMinor, cancellationToken);
    }

    public new async Task<SettlementInstruction> ExecuteInstructionAsync(
        Guid instructionId,
        CancellationToken cancellationToken)
    {
        ExecuteCalls++;
        return await base.ExecuteInstructionAsync(instructionId, cancellationToken);
    }
}

sealed class InMemoryRepository : ISettlementRepository
{
    private readonly Dictionary<Guid, SettlementInstruction> instructions = new();
    private readonly Dictionary<Guid, Settlement.Domain.Batches.SettlementBatch> batches = new();

    public Task SaveInstructionAsync(SettlementInstruction instruction, CancellationToken cancellationToken)
    {
        instructions[instruction.InstructionId] = instruction;
        return Task.CompletedTask;
    }

    public Task<SettlementInstruction?> GetInstructionAsync(Guid instructionId, CancellationToken cancellationToken)
    {
        instructions.TryGetValue(instructionId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyCollection<SettlementInstruction>> GetInstructionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<SettlementInstruction>>(instructions.Values.ToArray());

    public Task SaveBatchAsync(Settlement.Domain.Batches.SettlementBatch batch, CancellationToken cancellationToken)
    {
        batches[batch.BatchId] = batch;
        return Task.CompletedTask;
    }

    public Task<Settlement.Domain.Batches.SettlementBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        batches.TryGetValue(batchId, out var value);
        return Task.FromResult(value);
    }
}

sealed class SameCurrencyFxProvider : IFxQuoteProvider
{
    public Task<FxQuote> GetQuoteAsync(
        string sourceCurrency,
        string destinationCurrency,
        long amountMinor,
        CancellationToken cancellationToken) =>
        Task.FromResult(new FxQuote(sourceCurrency, destinationCurrency, 1m, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5)));
}

sealed class AcceptingTreasuryGateway : ITreasurySettlementGateway
{
    public Task<bool> HasAvailableFundsAsync(Guid accountId, string currencyCode, long amountMinor, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task PostSettlementAsync(TreasurySettlementPosting posting, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
