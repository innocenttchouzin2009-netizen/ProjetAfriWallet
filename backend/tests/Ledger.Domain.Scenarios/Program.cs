using AfriWallet.Ledger.Domain;

await RunAsync("balanced two-line journal is accepted and normalized", () =>
{
    var cash = AccountId.New();
    var clearing = AccountId.New();
    var correlationId = Guid.NewGuid();
    var postedAt = DateTimeOffset.UtcNow;

    var journal = JournalEntry.Create(
        JournalEntryId.New(),
        " xaf ",
        " PAYMENT-001 ",
        correlationId,
        postedAt,
        [
            new LedgerLine(cash, LedgerSide.Debit, 125_000, "Cash debit"),
            new LedgerLine(clearing, LedgerSide.Credit, 125_000, "Clearing credit")
        ]);

    Assert(journal.CurrencyCode == "XAF", "Currency must be normalized.");
    Assert(journal.BusinessReference == "PAYMENT-001", "Business reference must be trimmed.");
    Assert(journal.CorrelationId == correlationId, "Correlation id must be preserved.");
    Assert(journal.Lines.Count == 2, "Journal must preserve both lines.");
});

await RunAsync("multi-line journal accepts equal debit and credit totals", () =>
{
    var journal = JournalEntry.Create(
        JournalEntryId.New(),
        "EUR",
        "BATCH-42",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        [
            new LedgerLine(AccountId.New(), LedgerSide.Debit, 7_500),
            new LedgerLine(AccountId.New(), LedgerSide.Debit, 2_500),
            new LedgerLine(AccountId.New(), LedgerSide.Credit, 10_000)
        ]);

    Assert(journal.Lines.Count == 3, "Three balanced lines should be accepted.");
});

await RunAsync("unbalanced journal is rejected", () =>
{
    AssertThrows<InvalidOperationException>(() => JournalEntry.Create(
        JournalEntryId.New(),
        "USD",
        "UNBALANCED",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        [
            new LedgerLine(AccountId.New(), LedgerSide.Debit, 10_000),
            new LedgerLine(AccountId.New(), LedgerSide.Credit, 9_999)
        ]));
});

await RunAsync("journal requires at least two lines", () =>
{
    AssertThrows<InvalidOperationException>(() => JournalEntry.Create(
        JournalEntryId.New(),
        "XAF",
        "ONE-LINE",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 1_000)]));
});

await RunAsync("ledger line rejects zero and negative amounts", () =>
{
    AssertThrows<ArgumentOutOfRangeException>(() => new LedgerLine(AccountId.New(), LedgerSide.Debit, 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new LedgerLine(AccountId.New(), LedgerSide.Credit, -1));
});

await RunAsync("journal identity currency reference and timestamp are validated", () =>
{
    AssertThrows<ArgumentException>(() => new JournalEntryId(Guid.Empty));
    AssertThrows<ArgumentException>(() => new AccountId(Guid.Empty));

    AssertThrows<ArgumentException>(() => JournalEntry.Create(
        JournalEntryId.New(), "EU", "REF", Guid.NewGuid(), DateTimeOffset.UtcNow,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 1), new LedgerLine(AccountId.New(), LedgerSide.Credit, 1)]));

    AssertThrows<ArgumentException>(() => JournalEntry.Create(
        JournalEntryId.New(), "EUR", " ", Guid.NewGuid(), DateTimeOffset.UtcNow,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 1), new LedgerLine(AccountId.New(), LedgerSide.Credit, 1)]));

    AssertThrows<ArgumentException>(() => JournalEntry.Create(
        JournalEntryId.New(), "EUR", "REF", Guid.Empty, DateTimeOffset.UtcNow,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 1), new LedgerLine(AccountId.New(), LedgerSide.Credit, 1)]));

    var nonUtc = new DateTimeOffset(2026, 9, 9, 20, 0, 0, TimeSpan.FromHours(2));
    AssertThrows<ArgumentException>(() => JournalEntry.Create(
        JournalEntryId.New(), "EUR", "REF", Guid.NewGuid(), nonUtc,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 1), new LedgerLine(AccountId.New(), LedgerSide.Credit, 1)]));
});

await RunAsync("journal exposes immutable line collection", () =>
{
    var source = new[]
    {
        new LedgerLine(AccountId.New(), LedgerSide.Debit, 500),
        new LedgerLine(AccountId.New(), LedgerSide.Credit, 500)
    };

    var journal = JournalEntry.Create(
        JournalEntryId.New(), "EUR", "IMMUTABLE", Guid.NewGuid(), DateTimeOffset.UtcNow, source);

    source[0] = new LedgerLine(AccountId.New(), LedgerSide.Debit, 999);
    Assert(journal.Lines[0].AmountMinor == 500, "Journal lines must be defensively copied.");
});

Console.WriteLine("Ledger domain scenarios passed.");

static Task RunAsync(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
    return Task.CompletedTask;
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
