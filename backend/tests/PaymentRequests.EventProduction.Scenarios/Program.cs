using System.Text.Json;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static PaymentRequest NewRequest(DateTimeOffset createdAt, RecipientReference reference, DateTimeOffset? expiresAt = null) =>
    PaymentRequest.Create(
        WalletId.From(Guid.NewGuid()),
        reference,
        Currency.Create("XAF"),
        2_500,
        Guid.NewGuid(),
        createdAt,
        expiresAt);

static PaymentRequestEventEnvelope Envelope(
    PaymentRequest request,
    PaymentRequestLifecycleEventKind kind,
    DateTimeOffset occurredAt) =>
    PaymentRequestLifecycleEventFactory.ToEnvelope(
        PaymentRequestLifecycleEventFactory.Create(request, kind, occurredAt));

var createdAt = new DateTimeOffset(2026, 9, 15, 19, 0, 0, TimeSpan.Zero);
var secretAfWalId = "payer.secret.afwal";
var created = NewRequest(createdAt, RecipientReference.FromAfWalId(secretAfWalId), createdAt.AddHours(1));
var createdEnvelope = Envelope(created, PaymentRequestLifecycleEventKind.Created, createdAt);
Assert(createdEnvelope.EventType == "payment-request.created", "Created event type mismatch.");
Assert(createdEnvelope.PaymentRequestId == created.Id, "Created request id mismatch.");
Assert(createdEnvelope.OccurredAtUtc == createdAt, "Created timestamp mismatch.");
Assert(!createdEnvelope.PayloadJson.Contains(secretAfWalId, StringComparison.Ordinal), "Payload must not expose AfWal ID value.");
using (var payload = JsonDocument.Parse(createdEnvelope.PayloadJson))
{
    var root = payload.RootElement;
    Assert(root.GetProperty("version").GetInt32() == 1, "Payload version mismatch.");
    Assert(root.GetProperty("status").GetString() == "Pending", "Created status mismatch.");
    Assert(root.GetProperty("currencyCode").GetString() == "XAF", "Currency mismatch.");
    Assert(root.GetProperty("amountMinor").GetInt64() == 2_500, "Amount mismatch.");
}

var payerWallet = WalletId.From(Guid.NewGuid());
var accepted = NewRequest(createdAt, RecipientReference.FromAfWalId("payer.one"), createdAt.AddHours(1));
accepted.Accept(payerWallet, createdAt.AddMinutes(1));
var acceptedEnvelope = Envelope(accepted, PaymentRequestLifecycleEventKind.Accepted, accepted.UpdatedAtUtc);
Assert(acceptedEnvelope.EventType == "payment-request.accepted", "Accepted event type mismatch.");
using (var payload = JsonDocument.Parse(acceptedEnvelope.PayloadJson))
{
    Assert(payload.RootElement.GetProperty("acceptedPayerWalletId").GetGuid() == payerWallet.Value, "Accepted payer wallet mismatch.");
}

var declined = NewRequest(createdAt, RecipientReference.FromQrToken("opaque-qr-secret"), createdAt.AddHours(1));
declined.Decline(createdAt.AddMinutes(2));
var declinedEnvelope = Envelope(declined, PaymentRequestLifecycleEventKind.Declined, declined.UpdatedAtUtc);
Assert(declinedEnvelope.EventType == "payment-request.declined", "Declined event type mismatch.");
Assert(!declinedEnvelope.PayloadJson.Contains("opaque-qr-secret", StringComparison.Ordinal), "Payload must not expose raw QR token.");

var cancelled = NewRequest(createdAt, RecipientReference.FromAfWalId("payer.cancel"), createdAt.AddHours(1));
cancelled.Cancel(createdAt.AddMinutes(3));
Assert(Envelope(cancelled, PaymentRequestLifecycleEventKind.Cancelled, cancelled.UpdatedAtUtc).EventType == "payment-request.cancelled", "Cancelled event type mismatch.");

var expired = NewRequest(createdAt, RecipientReference.FromAfWalId("payer.expire"), createdAt.AddMinutes(5));
expired.Expire(createdAt.AddMinutes(5));
Assert(Envelope(expired, PaymentRequestLifecycleEventKind.Expired, expired.UpdatedAtUtc).EventType == "payment-request.expired", "Expired event type mismatch.");

var paid = NewRequest(createdAt, RecipientReference.FromAfWalId("payer.pay"), createdAt.AddHours(1));
var paidWallet = WalletId.From(Guid.NewGuid());
paid.Accept(paidWallet, createdAt.AddMinutes(1));
var transferId = Guid.NewGuid();
paid.MarkPaid(transferId, createdAt.AddMinutes(2));
var paidEnvelope = Envelope(paid, PaymentRequestLifecycleEventKind.Paid, paid.UpdatedAtUtc);
Assert(paidEnvelope.EventType == "payment-request.paid", "Paid event type mismatch.");
using (var payload = JsonDocument.Parse(paidEnvelope.PayloadJson))
{
    Assert(payload.RootElement.GetProperty("transferId").GetGuid() == transferId, "Paid transfer id mismatch.");
}

try
{
    _ = PaymentRequestLifecycleEventFactory.Create(created, PaymentRequestLifecycleEventKind.Paid, createdAt);
    throw new InvalidOperationException("Expected lifecycle status mismatch.");
}
catch (InvalidOperationException exception) when (exception.Message.Contains("cannot be emitted", StringComparison.Ordinal)) { }

try
{
    _ = PaymentRequestLifecycleEventFactory.Create(created, PaymentRequestLifecycleEventKind.Created, createdAt.AddSeconds(1));
    throw new InvalidOperationException("Expected created timestamp mismatch.");
}
catch (InvalidOperationException exception) when (exception.Message.Contains("creation time", StringComparison.Ordinal)) { }

Console.WriteLine("AFW-BE-REQUEST-EVENT-PRODUCTION-1 lifecycle event contract scenarios: PASS");
