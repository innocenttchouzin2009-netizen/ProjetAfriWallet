using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var requesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var currency = Currency.Create(" eur ");
var afWalId = RecipientReference.FromAfWalId("payer.afwal");
var createdAt = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
var expiresAt = createdAt.AddHours(24);
var correlationId = Guid.NewGuid();

var request = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    currency,
    12_500,
    correlationId,
    createdAt,
    expiresAt);

Assert(request.Id.Value != Guid.Empty, "Payment request id must be generated.");
Assert(request.Status == PaymentRequestStatus.Pending, "New request must be pending.");
Assert(request.Currency.Code == "EUR", "Currency normalization must be preserved.");
Assert(request.AmountMinor == 12_500, "Amount must be preserved.");
Assert(request.CorrelationId == correlationId, "Correlation id must be preserved.");
Assert(request.PayerReference == afWalId, "Recipient reference must be preserved.");
Assert(request.ExpiresAtUtc == expiresAt, "Expiration must be preserved.");

request.Accept(payerWallet, createdAt.AddMinutes(10));
Assert(request.Status == PaymentRequestStatus.Accepted, "Pending request must become accepted.");
Assert(request.AcceptedPayerWalletId == payerWallet, "Accepted payer wallet must be bound.");
Assert(request.AcceptedAtUtc == createdAt.AddMinutes(10), "Acceptance timestamp must be recorded.");

var transferId = Guid.NewGuid();
request.MarkPaid(transferId, createdAt.AddMinutes(15));
Assert(request.Status == PaymentRequestStatus.Paid, "Accepted request must become paid.");
Assert(request.TransferId == transferId, "Transfer id must be recorded.");
Assert(request.ClosedAtUtc == createdAt.AddMinutes(15), "Paid request must record close timestamp.");
AssertThrows<InvalidOperationException>(
    () => request.Cancel(createdAt.AddMinutes(20)),
    "Paid request must be terminal.");

var declined = PaymentRequest.Create(
    requesterWallet,
    RecipientReference.FromQrToken("opaque-qr-token"),
    Currency.Create("XAF"),
    2_000,
    Guid.NewGuid(),
    createdAt);
declined.Decline(createdAt.AddMinutes(1));
Assert(declined.Status == PaymentRequestStatus.Declined, "Pending request must be declinable.");
AssertThrows<InvalidOperationException>(
    () => declined.Accept(payerWallet, createdAt.AddMinutes(2)),
    "Declined request must be terminal.");

var cancelledPending = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
cancelledPending.Cancel(createdAt.AddMinutes(1));
Assert(cancelledPending.Status == PaymentRequestStatus.Cancelled, "Pending request must be cancellable.");

var cancelledAccepted = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
cancelledAccepted.Accept(payerWallet, createdAt.AddMinutes(1));
cancelledAccepted.Cancel(createdAt.AddMinutes(2));
Assert(cancelledAccepted.Status == PaymentRequestStatus.Cancelled, "Accepted request must be cancellable before payment.");

var expiring = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddMinutes(5));
AssertThrows<InvalidOperationException>(
    () => expiring.Expire(createdAt.AddMinutes(4)),
    "Request cannot expire before deadline.");
expiring.Expire(createdAt.AddMinutes(5));
Assert(expiring.Status == PaymentRequestStatus.Expired, "Request must expire at deadline.");

var lateAcceptance = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddMinutes(5));
AssertThrows<InvalidOperationException>(
    () => lateAcceptance.Accept(payerWallet, createdAt.AddMinutes(5)),
    "Expired request cannot be accepted.");

var noExpiry = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
AssertThrows<InvalidOperationException>(
    () => noExpiry.Expire(createdAt.AddHours(1)),
    "Request without expiration cannot be expired explicitly.");

var selfRequest = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
AssertThrows<InvalidOperationException>(
    () => selfRequest.Accept(requesterWallet, createdAt.AddMinutes(1)),
    "Requester and payer wallet must differ.");
AssertThrows<ArgumentException>(
    () => selfRequest.Accept(default, createdAt.AddMinutes(1)),
    "Empty payer wallet must be rejected.");

AssertThrows<ArgumentException>(
    () => PaymentRequest.Create(default, afWalId, Currency.Create("EUR"), 1_000, Guid.NewGuid(), createdAt),
    "Empty requester wallet must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => PaymentRequest.Create(requesterWallet, afWalId, Currency.Create("EUR"), 0, Guid.NewGuid(), createdAt),
    "Zero amount must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequest.Create(requesterWallet, afWalId, Currency.Create("EUR"), 1_000, Guid.Empty, createdAt),
    "Empty correlation id must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequest.Create(
        requesterWallet,
        afWalId,
        Currency.Create("EUR"),
        1_000,
        Guid.NewGuid(),
        new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.FromHours(2))),
    "Non-UTC creation timestamp must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequest.Create(
        requesterWallet,
        afWalId,
        Currency.Create("EUR"),
        1_000,
        Guid.NewGuid(),
        createdAt,
        createdAt),
    "Expiration must be later than creation.");

var chronological = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
AssertThrows<ArgumentException>(
    () => chronological.Accept(payerWallet, createdAt.AddSeconds(-1)),
    "Acceptance cannot predate creation.");
chronological.Accept(payerWallet, createdAt.AddMinutes(2));
AssertThrows<ArgumentException>(
    () => chronological.MarkPaid(Guid.NewGuid(), createdAt.AddMinutes(1)),
    "Payment cannot predate acceptance.");

var unpaid = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt);
AssertThrows<InvalidOperationException>(
    () => unpaid.MarkPaid(Guid.NewGuid(), createdAt.AddMinutes(1)),
    "Pending request cannot be marked paid.");

var acceptedWithExpiry = PaymentRequest.Create(
    requesterWallet,
    afWalId,
    Currency.Create("EUR"),
    1_000,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddMinutes(5));
acceptedWithExpiry.Accept(payerWallet, createdAt.AddMinutes(1));
AssertThrows<InvalidOperationException>(
    () => acceptedWithExpiry.MarkPaid(Guid.NewGuid(), createdAt.AddMinutes(5)),
    "Accepted request cannot be paid after expiration.");

Console.WriteLine("AFW-BE-REQUEST-1 payment request domain/lifecycle scenarios: PASS");
