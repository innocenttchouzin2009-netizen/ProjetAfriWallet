using AfriWallet.P2P.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

var ownerId = Guid.NewGuid();
var eur = Currency.Create("EUR");
var xaf = Currency.Create("XAF");
var activeEur = Wallet.Create(WalletId.New(), ownerId, eur, null, DateTimeOffset.UtcNow);
var activeXaf = Wallet.Create(WalletId.New(), ownerId, xaf, null, DateTimeOffset.UtcNow);
var repository = new RecordingWalletRepository(activeEur, activeXaf);
var selector = new WalletRecipientSelector(repository);

var afWalDirectory = new RecordingAfWalDirectory(ownerId);
var afWalLookup = new AfWalIdRecipientLookup(afWalDirectory, selector);
var resolvedAfWal = await afWalLookup.ResolveAsync("leaty.237", eur);
Assert(resolvedAfWal == activeEur.Id, "AfWal ID resolves active wallet for requested currency");
Assert(afWalDirectory.LastValue == "leaty.237", "AfWal ID forwarded unchanged to authoritative directory");
Assert(repository.ListCalls == 1, "wallet registry queried after AfWal identity resolution");

var resolvedXaf = await afWalLookup.ResolveAsync("leaty.237", xaf);
Assert(resolvedXaf == activeXaf.Id, "wallet selector chooses requested currency");

var qrDirectory = new RecordingQrDirectory(ownerId);
var qrLookup = new QrRecipientLookup(qrDirectory, selector);
var resolvedQr = await qrLookup.ResolveAsync("opaque-token", eur);
Assert(resolvedQr == activeEur.Id, "QR token resolves active wallet for requested currency");
Assert(qrDirectory.LastValue == "opaque-token", "QR token remains opaque and is forwarded unchanged");

var missingRepository = new RecordingWalletRepository(activeEur);
var missingSelector = new WalletRecipientSelector(missingRepository);
var missingAfWal = new AfWalIdRecipientLookup(new RecordingAfWalDirectory(null), missingSelector);
var missingAfWalResult = await missingAfWal.ResolveAsync("unknown", eur);
Assert(missingAfWalResult is null, "unknown AfWal identity returns not found");
Assert(missingRepository.ListCalls == 0, "wallet registry not queried when AfWal identity is unknown");

var missingQrRepository = new RecordingWalletRepository(activeEur);
var missingQr = new QrRecipientLookup(new RecordingQrDirectory(null), new WalletRecipientSelector(missingQrRepository));
var missingQrResult = await missingQr.ResolveAsync("unknown-token", eur);
Assert(missingQrResult is null, "unknown QR identity returns not found");
Assert(missingQrRepository.ListCalls == 0, "wallet registry not queried when QR identity is unknown");

var suspended = Wallet.Create(WalletId.New(), ownerId, eur, null, DateTimeOffset.UtcNow);
suspended.Suspend(DateTimeOffset.UtcNow.AddSeconds(1));
var suspendedLookup = new AfWalIdRecipientLookup(
    new RecordingAfWalDirectory(ownerId),
    new WalletRecipientSelector(new RecordingWalletRepository(suspended)));
Assert(await suspendedLookup.ResolveAsync("leaty.237", eur) is null, "suspended recipient wallet is not selectable");

var noCurrencyLookup = new AfWalIdRecipientLookup(
    new RecordingAfWalDirectory(ownerId),
    new WalletRecipientSelector(new RecordingWalletRepository(activeXaf)));
Assert(await noCurrencyLookup.ResolveAsync("leaty.237", eur) is null, "missing requested currency returns not found");

var duplicateA = Wallet.Create(WalletId.New(), ownerId, eur, null, DateTimeOffset.UtcNow);
var duplicateB = Wallet.Create(WalletId.New(), ownerId, eur, null, DateTimeOffset.UtcNow);
var ambiguousLookup = new AfWalIdRecipientLookup(
    new RecordingAfWalDirectory(ownerId),
    new WalletRecipientSelector(new RecordingWalletRepository(duplicateA, duplicateB)));
await AssertThrowsAsync<InvalidOperationException>(
    () => ambiguousLookup.ResolveAsync("leaty.237", eur),
    "ambiguous active wallets fail closed");

var corruptDirectoryLookup = new AfWalIdRecipientLookup(
    new RecordingAfWalDirectory(Guid.Empty),
    selector);
await AssertThrowsAsync<InvalidOperationException>(
    () => corruptDirectoryLookup.ResolveAsync("leaty.237", eur),
    "empty owner id from identity directory rejected");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => afWalLookup.ResolveAsync("leaty.237", eur, cts.Token),
    "AfWal lookup cancellation propagated");
await AssertThrowsAsync<OperationCanceledException>(
    () => qrLookup.ResolveAsync("opaque-token", eur, cts.Token),
    "QR lookup cancellation propagated");

Console.WriteLine("AFW-BE-P2P-1 concrete recipient resolution adapter scenarios: PASS");

static void Assert(bool condition, string scenario)
{
    if (!condition) throw new InvalidOperationException($"Scenario failed: {scenario}");
    Console.WriteLine($"PASS: {scenario}");
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string scenario) where TException : Exception
{
    try { await action(); }
    catch (TException) { Console.WriteLine($"PASS: {scenario}"); return; }
    throw new InvalidOperationException($"Scenario failed: {scenario}");
}

sealed class RecordingAfWalDirectory(Guid? ownerId) : IAfWalIdentityDirectory
{
    public string? LastValue { get; private set; }

    public Task<Guid?> ResolveOwnerIdAsync(string afWalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastValue = afWalId;
        return Task.FromResult(ownerId);
    }
}

sealed class RecordingQrDirectory(Guid? ownerId) : IQrRecipientDirectory
{
    public string? LastValue { get; private set; }

    public Task<Guid?> ResolveOwnerIdAsync(string qrToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastValue = qrToken;
        return Task.FromResult(ownerId);
    }
}

sealed class RecordingWalletRepository(params Wallet[] wallets) : IWalletRepository
{
    private readonly List<Wallet> _wallets = [.. wallets];
    public int ListCalls { get; private set; }

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default)
        => Task.FromResult(_wallets.Any(wallet => wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets.Add(wallet);
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
        => Task.FromResult(_wallets.SingleOrDefault(wallet => wallet.Id == walletId));

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ListCalls++;
        IReadOnlyList<Wallet> result = _wallets.Where(wallet => wallet.OwnerId == ownerId).ToArray();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
