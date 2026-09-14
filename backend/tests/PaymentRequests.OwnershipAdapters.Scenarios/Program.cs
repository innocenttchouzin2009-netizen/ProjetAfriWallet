using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Infrastructure;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var ownerId = Guid.NewGuid();
var foreignOwnerId = Guid.NewGuid();
var walletA = Wallet.Create(WalletId.From(Guid.NewGuid()), ownerId, Currency.Create("XAF"), null, DateTimeOffset.UtcNow);
var walletB = Wallet.Create(WalletId.From(Guid.NewGuid()), ownerId, Currency.Create("EUR"), null, DateTimeOffset.UtcNow);
var foreignWallet = Wallet.Create(WalletId.From(Guid.NewGuid()), foreignOwnerId, Currency.Create("XAF"), null, DateTimeOffset.UtcNow);

var walletReader = new WalletRegistryOwnedWalletReader(new InMemoryWalletRepository([walletA, walletB, foreignWallet]));
var ownedWalletIds = await walletReader.ListOwnedWalletIdsAsync(ownerId);
Assert(ownedWalletIds.Count == 2, "Owned wallet reader must return only wallets owned by the user.");
Assert(ownedWalletIds.Contains(walletA.Id) && ownedWalletIds.Contains(walletB.Id), "Owned wallet ids must be preserved.");
Assert(!ownedWalletIds.Contains(foreignWallet.Id), "Foreign wallet must never enter ownership scope.");

await using var directoryConnection = new SqliteConnection("Data Source=:memory:");
await directoryConnection.OpenAsync();
var directoryOptions = new DbContextOptionsBuilder<RecipientDirectoryDbContext>()
    .UseSqlite(directoryConnection)
    .Options;
await using var directoryDb = new RecipientDirectoryDbContext(directoryOptions);
await directoryDb.Database.EnsureCreatedAsync();

await using var requestConnection = new SqliteConnection("Data Source=:memory:");
await requestConnection.OpenAsync();
var requestOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(requestConnection)
    .Options;
await using var requestDb = new PaymentRequestDbContext(requestOptions);
await requestDb.Database.EnsureCreatedAsync();

var ownedAfWalId = "owner.afwal";
var inactiveAfWalId = "owner.inactive";
var foreignAfWalId = "foreign.afwal";
var ownedQrToken = "opaque-owned-qr-token";
var inactiveQrToken = "opaque-inactive-qr-token";
var foreignQrToken = "opaque-foreign-qr-token";

directoryDb.AfWalIdentities.AddRange(
    new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = ownerId, AfWalId = ownedAfWalId, IsActive = true },
    new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = ownerId, AfWalId = inactiveAfWalId, IsActive = false },
    new AfWalIdentityEntry { Id = Guid.NewGuid(), OwnerId = foreignOwnerId, AfWalId = foreignAfWalId, IsActive = true });

directoryDb.QrRecipients.AddRange(
    new QrRecipientEntry { Id = Guid.NewGuid(), OwnerId = ownerId, TokenHash = RecipientDirectoryNormalization.HashQrToken(ownedQrToken), IsActive = true },
    new QrRecipientEntry { Id = Guid.NewGuid(), OwnerId = ownerId, TokenHash = RecipientDirectoryNormalization.HashQrToken(inactiveQrToken), IsActive = false },
    new QrRecipientEntry { Id = Guid.NewGuid(), OwnerId = foreignOwnerId, TokenHash = RecipientDirectoryNormalization.HashQrToken(foreignQrToken), IsActive = true });
await directoryDb.SaveChangesAsync();

requestDb.PaymentRequests.AddRange(
    RequestWithQr(ownedQrToken),
    RequestWithQr(inactiveQrToken),
    RequestWithQr(foreignQrToken));
await requestDb.SaveChangesAsync();

var referenceReader = new AuthoritativeRecipientReferenceReader(directoryDb, requestDb);
var ownedReferences = await referenceReader.ListOwnedRecipientReferencesAsync(ownerId);
var expectedAfWal = RecipientReference.FromAfWalId(ownedAfWalId);
var expectedQr = RecipientReference.FromQrToken(ownedQrToken);
Assert(ownedReferences.Contains(expectedAfWal), "Active owned AfWal ID must be included.");
Assert(ownedReferences.Contains(expectedQr), "Active owned QR token must be recovered only through authoritative hash ownership.");
Assert(!ownedReferences.Contains(RecipientReference.FromAfWalId(inactiveAfWalId)), "Inactive AfWal ID must be excluded.");
Assert(!ownedReferences.Contains(RecipientReference.FromAfWalId(foreignAfWalId)), "Foreign AfWal ID must be excluded.");
Assert(!ownedReferences.Contains(RecipientReference.FromQrToken(inactiveQrToken)), "Inactive QR must be excluded.");
Assert(!ownedReferences.Contains(RecipientReference.FromQrToken(foreignQrToken)), "Foreign QR must be excluded.");

var qrOnlyOwner = Guid.NewGuid();
var qrNeverUsed = "opaque-never-used-qr";
directoryDb.QrRecipients.Add(new QrRecipientEntry
{
    Id = Guid.NewGuid(),
    OwnerId = qrOnlyOwner,
    TokenHash = RecipientDirectoryNormalization.HashQrToken(qrNeverUsed),
    IsActive = true
});
await directoryDb.SaveChangesAsync();
var neverUsedReferences = await referenceReader.ListOwnedRecipientReferencesAsync(qrOnlyOwner);
Assert(neverUsedReferences.Count == 0, "A QR hash without persisted raw request material must fail closed rather than invent a token.");

await AssertThrowsAsync<ArgumentException>(
    () => walletReader.ListOwnedWalletIdsAsync(Guid.Empty),
    "Empty user id must be rejected by wallet adapter.");
await AssertThrowsAsync<ArgumentException>(
    () => referenceReader.ListOwnedRecipientReferencesAsync(Guid.Empty),
    "Empty user id must be rejected by recipient reference adapter.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => walletReader.ListOwnedWalletIdsAsync(ownerId, cts.Token),
    "Wallet adapter must propagate cancellation.");
await AssertThrowsAsync<OperationCanceledException>(
    () => referenceReader.ListOwnedRecipientReferencesAsync(ownerId, cts.Token),
    "Recipient reference adapter must propagate cancellation.");

Console.WriteLine("AFW-BE-REQUEST-INBOX-1 concrete ownership scope adapter scenarios: PASS");

static PaymentRequestEntity RequestWithQr(string token) => new()
{
    Id = Guid.NewGuid(),
    RequesterWalletId = Guid.NewGuid(),
    PayerReferenceKind = (int)RecipientReferenceKind.QrToken,
    PayerReferenceValue = token,
    CurrencyCode = "XAF",
    AmountMinor = 1000,
    CorrelationId = Guid.NewGuid(),
    CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
    UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
    Status = 1
};

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(wallet => wallet.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(wallet => wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(walletId.Value, out var wallet) ? wallet : null);

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
