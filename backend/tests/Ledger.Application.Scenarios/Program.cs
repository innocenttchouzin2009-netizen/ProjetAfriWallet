using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;

var repository = new InMemoryJournalRepository();
var service = new LedgerPostingApplicationService(repository);
var debitAccount = Guid.NewGuid();
var creditAccount = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var postedAtUtc = DateTimeOffset.UtcNow;

await RunAsync("balanced journal posts successfully", async () =>
{
    var result = await service.PostAsync(
        new PostJournalCommand(
            "xaf",
            " payment-001 ",
            correlationId,
            new[]
            {
                new PostLedgerLineCommand(debitAccount, LedgerSide.Debit, 25_000, "debit"),
                new PostLedgerLineCommand(creditAccount, LedgerSide.Credit, 25_000, "credit")
            }),
        postedAtUtc);

    Assert(result.Succeeded, result.ErrorMessage ?? "Post should succeed.");
    Assert(result.Value is not null, "Posted journal view is required.");
    Assert(result.Value!.CurrencyCode == "XAF", "Currency must be normalized by the domain.");
    Assert(result.Value.BusinessReference == "payment-001", "Business reference must be normalized.");
    Assert(result.Value.CorrelationId == correlationId, "Correlation id must be preserved.");
    Assert(result.Value.Lines.Count == 2, "Two ledger lines are expected.");
});

await RunAsync("duplicate correlation is rejected", async () =>
{
    var result = await service.PostAsync(
        new PostJournalCommand(
            "XAF",
            "payment-duplicate",
            correlationId,
            new[]
            {
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Debit, 10_000),
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Credit, 10_000)
            }),
        postedAtUtc);

    Assert(!result.Succeeded, "Duplicate correlation must fail.");
    Assert(result.ErrorCode == LedgerErrorCode.DuplicateCorrelation, "Duplicate correlation error code expected.");
});

await RunAsync("unbalanced journal is rejected by domain invariant", async () =>
{
    var result = await service.PostAsync(
        new PostJournalCommand(
            "EUR",
            "payment-unbalanced",
            Guid.NewGuid(),
            new[]
            {
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Debit, 5_000),
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Credit, 4_999)
            }),
        postedAtUtc);

    Assert(!result.Succeeded, "Unbalanced journal must fail.");
    Assert(result.ErrorCode == LedgerErrorCode.ValidationError, "Validation error expected.");
});

await RunAsync("invalid account id is rejected", async () =>
{
    var result = await service.PostAsync(
        new PostJournalCommand(
            "USD",
            "payment-invalid-account",
            Guid.NewGuid(),
            new[]
            {
                new PostLedgerLineCommand(Guid.Empty, LedgerSide.Debit, 100),
                new PostLedgerLineCommand(Guid.NewGuid(), LedgerSide.Credit, 100)
            }),
        postedAtUtc);

    Assert(!result.Succeeded, "Empty account id must fail.");
    Assert(result.ErrorCode == LedgerErrorCode.ValidationError, "Validation error expected.");
});

await RunAsync("posted journal can be retrieved by id and correlation", async () =>
{
    var byCorrelation = await service.GetByCorrelationIdAsync(correlationId);
    Assert(byCorrelation.Succeeded && byCorrelation.Value is not null, "Journal must be retrievable by correlation.");

    var byId = await service.GetAsync(byCorrelation.Value!.JournalEntryId);
    Assert(byId.Succeeded && byId.Value is not null, "Journal must be retrievable by id.");
    Assert(byId.Value!.CorrelationId == correlationId, "Retrieved journal must match original correlation.");
});

await RunAsync("unknown journal returns not found", async () =>
{
    var result = await service.GetAsync(Guid.NewGuid());
    Assert(!result.Succeeded, "Unknown journal must fail.");
    Assert(result.ErrorCode == LedgerErrorCode.NotFound, "Not found error expected.");
});

Console.WriteLine("Ledger Application scenarios passed.");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class InMemoryJournalRepository : IJournalRepository
{
    private readonly Dictionary<Guid, JournalEntry> journals = new();

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(journals.Values.Any(journal => journal.CorrelationId == correlationId));

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        journals.Add(journalEntry.Id.Value, journalEntry);
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        journals.TryGetValue(journalEntryId.Value, out var journal);
        return Task.FromResult(journal);
    }

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(journals.Values.SingleOrDefault(journal => journal.CorrelationId == correlationId));
}
