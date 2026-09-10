using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;

var scenarios = new (string Name, Action Run)[]
{
    ("empty ledger returns zero for requested account", EmptyLedgerReturnsZero),
    ("single journal projects debit and credit totals", SingleJournalProjectsTotals),
    ("multiple journals accumulate deterministically", MultipleJournalsAccumulate),
    ("currencies stay isolated", CurrenciesStayIsolated),
    ("all accounts are projected independently", AccountsProjectIndependently),
    ("currency key is normalized", CurrencyKeyIsNormalized),
    ("projection overflow is rejected", ProjectionOverflowIsRejected)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Balance projection scenarios passed: {scenarios.Length}/{scenarios.Length}");

static void EmptyLedgerReturnsZero()
{
    var service = new BalanceProjectionService();
    var key = new BalanceKey(AccountId.New(), "XAF");

    var snapshot = service.Project(key, Array.Empty<JournalEntry>());

    Assert(snapshot.DebitMinor == 0, "Expected zero debit.");
    Assert(snapshot.CreditMinor == 0, "Expected zero credit.");
    Assert(snapshot.NetMinor == 0, "Expected zero net.");
}

static void SingleJournalProjectsTotals()
{
    var service = new BalanceProjectionService();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var journal = Journal("XAF", new LedgerLine(account, LedgerSide.Debit, 12_500), new LedgerLine(counterparty, LedgerSide.Credit, 12_500));

    var snapshot = service.Project(new BalanceKey(account, "XAF"), [journal]);

    Assert(snapshot.DebitMinor == 12_500, "Debit total mismatch.");
    Assert(snapshot.CreditMinor == 0, "Credit total mismatch.");
    Assert(snapshot.NetMinor == -12_500, "Signed net must be Credit - Debit.");
}

static void MultipleJournalsAccumulate()
{
    var service = new BalanceProjectionService();
    var account = AccountId.New();
    var counterparty = AccountId.New();
    var journals = new[]
    {
        Journal("EUR", new LedgerLine(account, LedgerSide.Credit, 8_000), new LedgerLine(counterparty, LedgerSide.Debit, 8_000)),
        Journal("EUR", new LedgerLine(account, LedgerSide.Debit, 2_500), new LedgerLine(counterparty, LedgerSide.Credit, 2_500)),
        Journal("EUR", new LedgerLine(account, LedgerSide.Credit, 1_000), new LedgerLine(counterparty, LedgerSide.Debit, 1_000))
    };

    var snapshot = service.Project(new BalanceKey(account, "EUR"), journals);

    Assert(snapshot.DebitMinor == 2_500, "Accumulated debit mismatch.");
    Assert(snapshot.CreditMinor == 9_000, "Accumulated credit mismatch.");
    Assert(snapshot.NetMinor == 6_500, "Accumulated net mismatch.");
}

static void CurrenciesStayIsolated()
{
    var service = new BalanceProjectionService();
    var account = AccountId.New();
    var xafCounterparty = AccountId.New();
    var eurCounterparty = AccountId.New();
    var journals = new[]
    {
        Journal("XAF", new LedgerLine(account, LedgerSide.Credit, 50_000), new LedgerLine(xafCounterparty, LedgerSide.Debit, 50_000)),
        Journal("EUR", new LedgerLine(account, LedgerSide.Credit, 3_000), new LedgerLine(eurCounterparty, LedgerSide.Debit, 3_000))
    };

    var xaf = service.Project(new BalanceKey(account, "XAF"), journals);
    var eur = service.Project(new BalanceKey(account, "EUR"), journals);

    Assert(xaf.CreditMinor == 50_000 && xaf.NetMinor == 50_000, "XAF projection leaked currency.");
    Assert(eur.CreditMinor == 3_000 && eur.NetMinor == 3_000, "EUR projection leaked currency.");
}

static void AccountsProjectIndependently()
{
    var service = new BalanceProjectionService();
    var accountA = AccountId.New();
    var accountB = AccountId.New();
    var journal = Journal("USD", new LedgerLine(accountA, LedgerSide.Debit, 4_200), new LedgerLine(accountB, LedgerSide.Credit, 4_200));

    var snapshots = service.Project([journal]);

    Assert(snapshots.Count == 2, "Expected one projection per account/currency key.");
    var a = snapshots.Single(snapshot => snapshot.Key.AccountId == accountA);
    var b = snapshots.Single(snapshot => snapshot.Key.AccountId == accountB);
    Assert(a.NetMinor == -4_200, "Account A projection mismatch.");
    Assert(b.NetMinor == 4_200, "Account B projection mismatch.");
}

static void CurrencyKeyIsNormalized()
{
    var key = new BalanceKey(AccountId.New(), " xaf ");
    Assert(key.CurrencyCode == "XAF", "Balance key currency must be normalized.");
}

static void ProjectionOverflowIsRejected()
{
    var service = new BalanceProjectionService();
    var account = AccountId.New();
    var counterpartyA = AccountId.New();
    var counterpartyB = AccountId.New();
    var journals = new[]
    {
        Journal("XAF", new LedgerLine(account, LedgerSide.Credit, long.MaxValue), new LedgerLine(counterpartyA, LedgerSide.Debit, long.MaxValue)),
        Journal("XAF", new LedgerLine(account, LedgerSide.Credit, 1), new LedgerLine(counterpartyB, LedgerSide.Debit, 1))
    };

    AssertThrows<OverflowException>(() => service.Project(new BalanceKey(account, "XAF"), journals));
}

static JournalEntry Journal(string currency, params LedgerLine[] lines) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currency,
        $"BAL-SCENARIO-{Guid.NewGuid():N}",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        lines);

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

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}
