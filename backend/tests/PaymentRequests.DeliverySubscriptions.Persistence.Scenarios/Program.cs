using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.DeliverySubscriptions.Persistence;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

var now = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero);
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<PaymentRequestDeliverySubscriptionDbContext>()
    .UseSqlite(connection).Options;
await using var db = new PaymentRequestDeliverySubscriptionDbContext(options);
await db.Database.EnsureCreatedAsync();

var registry = new EfPaymentRequestDeliverySubscriptionRegistry(db);
var service = new PaymentRequestDeliverySubscriptionService(registry);
var owner = Guid.NewGuid();
var recipient = RecipientReference.FromAfWalId("payer.afwal");
var endpoint = new Uri("https://merchant.example.test/hooks/payment-requests");
var retry = new PaymentRequestDeliveryRetryPolicy(7, TimeSpan.FromSeconds(45));
var created = await service.CreateAsync(
    owner, recipient, endpoint, "key-2026-09", "AFW_WEBHOOK_SECRET",
    new[] { "payment-request.created", "payment-request.paid" }, retry, now);

var roundTrip = await registry.GetAsync(created.Id);
Assert(roundTrip is not null && roundTrip.OwnerId == owner, "Owner must round-trip.");
Assert(roundTrip!.Recipient == PaymentRequestDeliveryRecipientBinding.From(recipient), "Recipient binding must round-trip.");
Assert(roundTrip.Endpoint == endpoint, "Configured endpoint must round-trip.");
Assert(roundTrip.RetryPolicy == retry, "Retry policy must round-trip.");
Assert(roundTrip.Authorizes("PAYMENT-REQUEST.PAID"), "Event authorization must normalize.");
Assert(!roundTrip.Authorizes("payment-request.cancelled"), "Unconfigured event must not be authorized.");

var active = await registry.ListActiveAsync(PaymentRequestDeliveryRecipientBinding.From(recipient), "payment-request.paid");
Assert(active.Count == 1 && active[0].Id == created.Id, "Active authorized subscription must be selected.");
Assert(await service.GetOwnedAsync(Guid.NewGuid(), created.Id) is null, "Foreign owner must not read subscription.");

Assert(await service.SetActiveAsync(owner, created.Id, false, now.AddMinutes(1)), "Owner must disable subscription.");
Assert((await registry.ListActiveAsync(PaymentRequestDeliveryRecipientBinding.From(recipient), "payment-request.paid")).Count == 0,
    "Disabled subscription must not be selected.");
Assert(await service.SetActiveAsync(owner, created.Id, true, now.AddMinutes(2)), "Owner must re-enable subscription.");

var qrToken = "opaque-sensitive-qr-token";
var qr = await service.CreateAsync(
    owner, RecipientReference.FromQrToken(qrToken),
    new Uri("https://merchant.example.test/hooks/qr"), "key-qr", "AFW_QR_WEBHOOK_SECRET",
    new[] { "payment-request.accepted" },
    PaymentRequestDeliveryRetryPolicy.Default, now);
var qrRow = await db.Subscriptions.AsNoTracking().SingleAsync(x => x.Id == qr.Id);
Assert(!qrRow.RecipientKey.Contains(qrToken, StringComparison.Ordinal), "Raw QR token must never be persisted.");
Assert(qrRow.RecipientKind == "qr-sha256" && qrRow.RecipientKey.Length == 64, "QR binding must be SHA-256.");

Throws<ArgumentException>(() => PaymentRequestDeliverySubscription.Create(
    owner, recipient, new Uri("ftp://example.test/hook"), "key", "AFW_WEBHOOK_SECRET",
    new[] { "payment-request.paid" }, retry, now));
Throws<ArgumentException>(() => PaymentRequestDeliverySubscription.Create(
    Guid.Empty, recipient, endpoint, "key", "AFW_WEBHOOK_SECRET",
    new[] { "payment-request.paid" }, retry, now));
Throws<ArgumentOutOfRangeException>(() =>
    new PaymentRequestDeliveryRetryPolicy(0, TimeSpan.FromSeconds(30)).Validate());

var dispatchProperties = typeof(AfriWallet.PaymentRequests.Application.PaymentRequestEventDispatch)
    .GetProperties().Select(x => x.Name).ToArray();
Assert(!dispatchProperties.Any(x => x.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
                                    x.Contains("Endpoint", StringComparison.OrdinalIgnoreCase)),
    "Business event dispatch must not accept URL/Endpoint fields.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try { await registry.GetAsync(created.Id, cts.Token); throw new InvalidOperationException("Expected cancellation."); }
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-WEBHOOK destination registry persistence scenarios: PASS");
