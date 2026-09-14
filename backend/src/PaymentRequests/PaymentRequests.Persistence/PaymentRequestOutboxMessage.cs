using System.Text.Json;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Persistence;

public static class PaymentRequestOutboxEventTypes
{
    public const string Created = "payment-request.created.v1";
    public const string Accepted = "payment-request.accepted.v1";
    public const string Paid = "payment-request.paid.v1";
    public const string Declined = "payment-request.declined.v1";
    public const string Cancelled = "payment-request.cancelled.v1";
    public const string Expired = "payment-request.expired.v1";
}

public sealed class PaymentRequestOutboxMessage
{
    public Guid Id { get; set; }
    public Guid PaymentRequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string OccurredAtUtc { get; set; } = string.Empty;
    public string? PublishedAtUtc { get; set; }
}

internal static class PaymentRequestOutboxFactory
{
    public static PaymentRequestOutboxMessage Created(PaymentRequest request) =>
        Create(request, PaymentRequestOutboxEventTypes.Created, request.CreatedAtUtc);

    public static PaymentRequestOutboxMessage Transition(PaymentRequest request) =>
        Create(request, EventTypeFor(request.Status), request.UpdatedAtUtc);

    private static PaymentRequestOutboxMessage Create(
        PaymentRequest request,
        string eventType,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = new
        {
            paymentRequestId = request.Id.Value,
            requesterWalletId = request.RequesterWalletId.Value,
            acceptedPayerWalletId = request.AcceptedPayerWalletId?.Value,
            currencyCode = request.Currency.Code,
            amountMinor = request.AmountMinor,
            correlationId = request.CorrelationId,
            status = request.Status.ToString(),
            transferId = request.TransferId,
            occurredAtUtc
        };

        return new PaymentRequestOutboxMessage
        {
            Id = Guid.NewGuid(),
            PaymentRequestId = request.Id.Value,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = occurredAtUtc.ToString("O"),
            PublishedAtUtc = null
        };
    }

    private static string EventTypeFor(PaymentRequestStatus status) => status switch
    {
        PaymentRequestStatus.Accepted => PaymentRequestOutboxEventTypes.Accepted,
        PaymentRequestStatus.Paid => PaymentRequestOutboxEventTypes.Paid,
        PaymentRequestStatus.Declined => PaymentRequestOutboxEventTypes.Declined,
        PaymentRequestStatus.Cancelled => PaymentRequestOutboxEventTypes.Cancelled,
        PaymentRequestStatus.Expired => PaymentRequestOutboxEventTypes.Expired,
        _ => throw new InvalidOperationException($"Status {status} does not produce a lifecycle transition outbox event.")
    };
}
