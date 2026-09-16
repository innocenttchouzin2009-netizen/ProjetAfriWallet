using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var requesterUserId = Guid.NewGuid();
var payerUserId = Guid.NewGuid();
var requesterWalletId = Guid.NewGuid();
var payerWalletId = Guid.NewGuid();
var requestId = Guid.NewGuid();
var transferId = Guid.NewGuid();
var occurredAt = new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

await using var requestConnection = new SqliteConnection("Data Source=:memory:");
await requestConnection.OpenAsync();
var requestOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(requestConnection)
    .Options;
await using var requestDb = new PaymentRequestDbContext(requestOptions);
await requestDb.Database.EnsureCreatedAsync();
requestDb.PaymentRequests.Add(new PaymentRequestEntity
{
    Id = requestId,
    RequesterWalletId = requesterWalletId,
    PayerReferenceKind = 1,
    PayerReferenceValue = "payer.afwal",
    CurrencyCode = "EUR",
    AmountMinor = 2500,
    CorrelationId = Guid.NewGuid(),
    CreatedAtUtc = occurredAt.ToString("O"),
    UpdatedAtUtc = occurredAt.ToString("O"),
    Status = 1
});
await requestDb.SaveChangesAsync();

await using var notificationConnection = new SqliteConnection("Data Source=:memory:");
await notificationConnection.OpenAsync();
var notificationOptions = new DbContextOptionsBuilder<NotificationInboxDbContext>()
    .UseSqlite(notificationConnection)
    .Options;
await using var notificationDb = new NotificationInboxDbContext(notificationOptions);
await notificationDb.Database.EnsureCreatedAsync();
var repository = new EfInAppNotificationRepository(notificationDb);

var wallets = new InMemoryWalletRepository([
    Wallet.Create(WalletId.From(requesterWalletId), requesterUserId, Currency.Create("EUR"), null, occurredAt),
    Wallet.Create(WalletId.From(payerWalletId), payerUserId, Currency.Create("EUR"), null, occurredAt)
]);
var transport = new InAppPaymentRequestEventTransport(
    requestDb,
    wallets,
    new FixedAfWalDirectory(payerUserId),
    new FixedQrDirectory(null),
    repository);

var createdEventId = Guid.NewGuid();
await transport.DispatchAsync(new PaymentRequestEventDispatch(
    createdEventId,
    requestId,
    "payment-request.created",
    occurredAt,
    "{}"));
Assert(await repository.CountUnreadAsync(payerUserId) == 1, "Created must notify payer.");
Assert(await repository.CountUnreadAsync(requesterUserId) == 0, "Created must not notify requester.");

await transport.DispatchAsync(new PaymentRequestEventDispatch(
    createdEventId,
    requestId,
    "payment-request.created",
    occurredAt,
    "{}"));
Assert(await repository.CountUnreadAsync(payerUserId) == 1, "Retry must dedupe by durable event id.");

var entity = await requestDb.PaymentRequests.SingleAsync(x => x.Id == requestId);
entity.AcceptedPayerWalletId = payerWalletId;
entity.Status = 2;
entity.UpdatedAtUtc = occurredAt.AddMinutes(1).ToString("O");
await requestDb.SaveChangesAsync();

await transport.DispatchAsync(new PaymentRequestEventDispatch(
    Guid.NewGuid(),
    requestId,
    "payment-request.accepted",
    occurredAt.AddMinutes(1),
    "{}"));
Assert(await repository.CountUnreadAsync(payerUserId) == 2, "Accepted must notify payer.");
Assert(await repository.CountUnreadAsync(requesterUserId) == 1, "Accepted must notify requester.");

entity.TransferId = transferId;
entity.Status = 6;
entity.UpdatedAtUtc = occurredAt.AddMinutes(2).ToString("O");
entity.ClosedAtUtc = occurredAt.AddMinutes(2).ToString("O");
await requestDb.SaveChangesAsync();

await transport.DispatchAsync(new PaymentRequestEventDispatch(
    Guid.NewGuid(),
    requestId,
    "payment-request.paid",
    occurredAt.AddMinutes(2),
    "{}"));
var payerItems = await repository.ListPageAsync(payerUserId, false, 10, null);
Assert(payerItems.Any(x => x.TransferId == transferId), "Paid notification must carry persisted transfer id.");
Assert(await repository.CountUnreadAsync(requesterUserId) == 2, "Paid must notify requester.");

try
{
    await transport.DispatchAsync(new PaymentRequestEventDispatch(
        Guid.NewGuid(), requestId, "payment-request.unknown", occurredAt, "{}"));
    throw new InvalidOperationException("Unsupported event type must fail permanently.");
}
catch (PaymentRequestEventTransportException exception)
    when (exception.FailureKind == PaymentRequestEventTransportFailureKind.Permanent)
{
}

Console.WriteLine("AFW-BE-NOTIFICATION-1 durable payment request event transport scenarios: PASS");

sealed class FixedAfWalDirectory(Guid? ownerId) : IAfWalIdentityDirectory
{
    public Task<Guid?> ResolveOwnerIdAsync(string afWalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ownerId);
    }
}

sealed class FixedQrDirectory(Guid? ownerId) : IQrRecipientDirectory
{
    public Task<Guid?> ResolveOwnerIdAsync(string qrToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ownerId);
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(walletId.Value, out var wallet) ? wallet : null);

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
