using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;

var service = new InternalTransferPlanningService();

Run("valid transfer produces balanced debit/credit plan", () =>
{
    var source = Context(" eur ", 50_000, true);
    var target = Context("EUR", 0, true);
    var result = service.Prepare(Command(source, target, 12_345));

    Assert(result.Intent.CurrencyCode == "EUR", "Currency must be normalized.");
    Assert(result.JournalEntry.CurrencyCode == "EUR", "Journal currency mismatch.");
    Assert(result.JournalEntry.Lines.Count == 2, "Transfer journal must have exactly two lines.");
    Assert(result.JournalEntry.Lines[0].Side == LedgerSide.Debit, "Source must be debited.");
    Assert(result.JournalEntry.Lines[1].Side == LedgerSide.Credit, "Target must be credited.");
    Assert(result.JournalEntry.Lines.All(x => x.AmountMinor == 12_345), "Journal amounts must match transfer amount.");
});

Run("same wallet is rejected", () =>
{
    var walletId = Guid.NewGuid();
    var source = Context("XAF", 10_000, true, walletId: walletId);
    var target = Context("XAF", 0, true, walletId: walletId);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Run("non-positive amount is rejected", () =>
{
    var source = Context("XAF", 10_000, true);
    var target = Context("XAF", 0, true);
    Expect<ArgumentOutOfRangeException>(() => service.Prepare(Command(source, target, 0)));
});

Run("inactive source wallet is rejected", () =>
{
    var source = Context("XAF", 10_000, false);
    var target = Context("XAF", 0, true);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Run("inactive target wallet is rejected", () =>
{
    var source = Context("XAF", 10_000, true);
    var target = Context("XAF", 0, false);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Run("currency mismatch is rejected until FX orchestration", () =>
{
    var source = Context("EUR", 10_000, true);
    var target = Context("XAF", 0, true);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Run("insufficient funds is rejected", () =>
{
    var source = Context("EUR", 999, true);
    var target = Context("EUR", 0, true);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Run("same ledger account is rejected", () =>
{
    var account = AccountId.New();
    var source = Context("EUR", 10_000, true, accountId: account);
    var target = Context("EUR", 0, true, accountId: account);
    Expect<InvalidOperationException>(() => service.Prepare(Command(source, target, 1_000)));
});

Console.WriteLine("Transfer Domain/Application scenarios passed.");

static TransferWalletContext Context(
    string currency,
    long available,
    bool active,
    Guid? walletId = null,
    AccountId? accountId = null) =>
    new(walletId ?? Guid.NewGuid(), accountId ?? AccountId.New(), currency, active, available);

static PrepareInternalTransferCommand Command(
    TransferWalletContext source,
    TransferWalletContext target,
    long amount) =>
    new(source, target, amount, Guid.NewGuid(), DateTimeOffset.UtcNow);

static void Run(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Expect<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(T).Name}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
