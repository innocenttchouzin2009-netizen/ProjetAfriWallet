using AfriWallet.Wallet.Domain;

Run("new wallet starts active with immutable identity and timestamps", () =>
{
    var now = new DateTimeOffset(2026, 9, 9, 20, 0, 0, TimeSpan.Zero);
    var id = WalletId.New();
    var ownerId = Guid.NewGuid();
    var wallet = Wallet.Create(id, ownerId, Currency.Create("xaf"), CountryCode.Create("cm"), now);

    Assert(wallet.Id == id, "Wallet id mismatch.");
    Assert(wallet.OwnerId == ownerId, "Owner id mismatch.");
    Assert(wallet.Currency.Code == "XAF", "Currency must be normalized.");
    Assert(wallet.CountryCode?.Value == "CM", "Country code must be normalized.");
    Assert(wallet.Status == WalletStatus.Active, "New wallet must be active.");
    Assert(wallet.CreatedAtUtc == now && wallet.UpdatedAtUtc == now, "Creation timestamps mismatch.");
});

Run("active wallet can suspend and suspended wallet can reactivate", () =>
{
    var wallet = CreateWallet();
    var suspendedAt = wallet.CreatedAtUtc.AddMinutes(1);
    wallet.Suspend(suspendedAt);
    Assert(wallet.Status == WalletStatus.Suspended, "Wallet must be suspended.");
    Assert(wallet.UpdatedAtUtc == suspendedAt, "Suspend timestamp mismatch.");

    var activatedAt = suspendedAt.AddMinutes(1);
    wallet.Activate(activatedAt);
    Assert(wallet.Status == WalletStatus.Active, "Wallet must be active again.");
    Assert(wallet.UpdatedAtUtc == activatedAt, "Activation timestamp mismatch.");
});

Run("closed wallet is terminal", () =>
{
    var wallet = CreateWallet();
    wallet.Close(wallet.CreatedAtUtc.AddMinutes(1));
    Assert(wallet.Status == WalletStatus.Closed, "Wallet must be closed.");
    AssertThrows<InvalidOperationException>(() => wallet.Activate(wallet.UpdatedAtUtc.AddMinutes(1)));
    AssertThrows<InvalidOperationException>(() => wallet.Suspend(wallet.UpdatedAtUtc.AddMinutes(1)));
    AssertThrows<InvalidOperationException>(() => wallet.Close(wallet.UpdatedAtUtc.AddMinutes(1)));
});

Run("suspended wallet can close", () =>
{
    var wallet = CreateWallet();
    wallet.Suspend(wallet.CreatedAtUtc.AddMinutes(1));
    wallet.Close(wallet.UpdatedAtUtc.AddMinutes(1));
    Assert(wallet.Status == WalletStatus.Closed, "Suspended wallet must be closable.");
});

Run("invalid lifecycle transitions are rejected", () =>
{
    var wallet = CreateWallet();
    AssertThrows<InvalidOperationException>(() => wallet.Activate(wallet.CreatedAtUtc.AddMinutes(1)));
    wallet.Suspend(wallet.CreatedAtUtc.AddMinutes(1));
    AssertThrows<InvalidOperationException>(() => wallet.Suspend(wallet.UpdatedAtUtc.AddMinutes(1)));
});

Run("lifecycle timestamps cannot move backwards", () =>
{
    var wallet = CreateWallet();
    AssertThrows<ArgumentOutOfRangeException>(() => wallet.Suspend(wallet.CreatedAtUtc.AddTicks(-1)));
});

Run("wallet identity and ISO-like codes reject invalid values", () =>
{
    AssertThrows<ArgumentException>(() => WalletId.From(Guid.Empty));
    AssertThrows<ArgumentException>(() => Wallet.Create(WalletId.New(), Guid.Empty, Currency.Create("EUR"), null, DateTimeOffset.UtcNow));
    AssertThrows<ArgumentException>(() => Currency.Create("EU"));
    AssertThrows<ArgumentException>(() => Currency.Create("€UR"));
    AssertThrows<ArgumentException>(() => CountryCode.Create("CMR"));
});

Console.WriteLine("Wallet domain scenarios passed.");

static Wallet CreateWallet() =>
    Wallet.Create(
        WalletId.New(),
        Guid.NewGuid(),
        Currency.Create("XAF"),
        CountryCode.Create("CM"),
        new DateTimeOffset(2026, 9, 9, 20, 0, 0, TimeSpan.Zero));

static void Run(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
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
