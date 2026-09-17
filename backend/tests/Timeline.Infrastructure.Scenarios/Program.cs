using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Timeline.Application;
using AfriWallet.Timeline.Domain;
using AfriWallet.Timeline.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var ownerId = Guid.NewGuid();
var otherOwnerId = Guid.NewGuid();
var walletId = WalletId.From(Guid.NewGuid());
var otherWalletId = WalletId.From(Guid.NewGuid());
var accountId = AccountId.New();
var otherAccountId = AccountId.New();
var now = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
var transferId = Guid.NewGuid();

await using var ledgerConnection = new SqliteConnection("Data Source=:memory:");
await ledgerConnection.OpenAsync();
var ledgerOptions = new DbContextOptionsBuilder<LedgerDbContext>().UseSqlite(ledgerConnection).Options;
await using var ledger = new LedgerDbContext(ledgerOptions);
await ledger.Database.EnsureCreatedAsync();

ledger.JournalEntries.Add(new LedgerJournalEntity
{
    Id = Guid.NewGuid(),
    CurrencyCode = "XAF",
    BusinessReference = $"TRF-{transferId}",
    CorrelationId = Guid.NewGuid(),
    PostedAtUtc = now.AddMinutes(3),
    Lines =
    [
        new LedgerLineEntity { Position = 0, AccountId = accountId.Value, Side = (int)LedgerSide.Debit, AmountMinor = 5_000 },
        new LedgerLineEntity { Position = 1, AccountId = otherAccountId.Value, Side = (int)LedgerSide.Credit, AmountMinor = 5_000 }
    ]
});
ledger.JournalEntries.Add(new LedgerJournalEntity
{
    Id = Guid.NewGuid(),
    CurrencyCode = "XAF",
    BusinessReference = "manual-adjustment",
    CorrelationId = Guid.NewGuid(),
    PostedAtUtc = now.AddMinutes(1),
    Lines =
    [
        new LedgerLineEntity { Position = 0, AccountId = accountId.Value, Side = (int)LedgerSide.Credit, AmountMinor = 900 },
        new LedgerLineEntity { Position = 1, AccountId = otherAccountId.Value, Side = (int)LedgerSide.Debit, AmountMinor = 900 }
    ]
});
await ledger.SaveChangesAsync();

await using var requestConnection = new SqliteConnection("Data Source=:memory:");
await requestConnection.OpenAsync();
var requestOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>().UseSqlite(requestConnection).Options;
await using var requests = new PaymentRequestDbContext(requestOptions);
await requests.Database.EnsureCreatedAsync();

requests.PaymentRequests.Add(new PaymentRequestEntity
{
    Id = Guid.NewGuid(),
    RequesterWalletId = walletId.Value,
    PayerReferenceKind = 1,
    PayerReferenceValue = "payer.one",
    CurrencyCode = "XAF",
    AmountMinor = 5_000,
    CorrelationId = Guid.NewGuid(),
    CreatedAtUtc = now.AddMinutes(-10).ToString("O"),
    UpdatedAtUtc = now.AddMinutes(3).ToString("O"),
    Status = (int)PaymentRequestStatus.Paid,
    AcceptedPayerWalletId = otherWalletId.Value,
    AcceptedAtUtc = now.ToString("O"),
    TransferId = transferId,
    ClosedAtUtc = now.AddMinutes(3).ToString("O")
});
var pendingRequestId = Guid.NewGuid();
requests.PaymentRequests.Add(new PaymentRequestEntity
{
    Id = pendingRequestId,
    RequesterWalletId = walletId.Value,
    PayerReferenceKind = 1,
    PayerReferenceValue = "payer.two",
    CurrencyCode = "EUR",
    AmountMinor = 2_000,
    CorrelationId = Guid.NewGuid(),
    CreatedAtUtc = now.AddMinutes(2).ToString("O"),
    UpdatedAtUtc = now.AddMinutes(2).ToString("O"),
    Status = (int)PaymentRequestStatus.Pending
});
await requests.SaveChangesAsync();

var wallets = new InMemoryWalletRepository([
    Wallet.Create(walletId, ownerId, Currency.Create("XAF"), null, now),
    Wallet.Create(otherWalletId, otherOwnerId, Currency.Create("XAF"), null, now)
]);

var reader = new AggregatingFinancialTimelineReader(
    ledger,
    requests,
    wallets,
    new Dictionary<Guid, AccountId>
    {
        [walletId.Value] = accountId,
        [otherWalletId.Value] = otherAccountId
    });

var firstPage = await reader.ReadAsync(FinancialTimelineQuery.Create(ownerId, limit: 2));
Assert(firstPage.Items.Count == 2, "First page must honor limit.");
Assert(firstPage.Items[0].Kind == FinancialTimelineEntryKind.MoneyTransfer, "Newest ledger transfer must be first.");
Assert(firstPage.Items[0].Direction == FinancialTimelineDirection.Outgoing, "Debit transfer must be outgoing.");
Assert(firstPage.Items[1].Kind == FinancialTimelineEntryKind.PaymentRequest, "Pending payment request must follow by occurrence time.");
Assert(firstPage.Items[1].Source.SourceId == pendingRequestId.ToString("N"), "Pending request must be preserved.");
Assert(firstPage.NextCursor is not null, "First page must expose cursor when more data exists.");
Assert(firstPage.Items.Count(item => item.Source.SourceSystem == "payment-request" && item.State == FinancialTimelineState.Completed) == 0,
    "Paid request sharing the transfer id must be deduplicated against ledger transfer.");

var secondPage = await reader.ReadAsync(FinancialTimelineQuery.Create(ownerId, limit: 2, cursor: firstPage.NextCursor));
Assert(secondPage.Items.Count == 1, "Second page must contain remaining item.");
Assert(secondPage.Items[0].Kind == FinancialTimelineEntryKind.LedgerPosting, "Remaining item must be generic ledger posting.");
Assert(secondPage.NextCursor is null, "Last page must not expose cursor.");

var walletFiltered = await reader.ReadAsync(FinancialTimelineQuery.Create(ownerId, walletId, limit: 10));
Assert(walletFiltered.Items.All(item => item.WalletId == walletId), "Wallet filter must constrain every item.");

var foreignWallet = await reader.ReadAsync(FinancialTimelineQuery.Create(ownerId, otherWalletId, limit: 10));
Assert(foreignWallet.Items.Count == 0, "Foreign wallet filter must not leak timeline items.");

try
{
    await reader.ReadAsync(FinancialTimelineQuery.Create(ownerId, limit: 10, cursor: "not-base64"));
    throw new InvalidOperationException("Expected invalid cursor rejection.");
}
catch (ArgumentException) { }

Console.WriteLine("AFW-BE-TIMELINE-1 source aggregation, deduplication, sorting and pagination scenarios: PASS");

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

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
