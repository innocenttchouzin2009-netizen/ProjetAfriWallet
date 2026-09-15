using System.Text.Json;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public static class PaymentRequestLifecycleEventFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PaymentRequestLifecycleEvent Create(
        PaymentRequest request,
        PaymentRequestLifecycleEventKind kind,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Lifecycle event timestamp must be UTC.", nameof(occurredAtUtc));
        }

        var expectedStatus = kind switch
        {
            PaymentRequestLifecycleEventKind.Created => PaymentRequestStatus.Pending,
            PaymentRequestLifecycleEventKind.Accepted => PaymentRequestStatus.Accepted,
            PaymentRequestLifecycleEventKind.Declined => PaymentRequestStatus.Declined,
            PaymentRequestLifecycleEventKind.Cancelled => PaymentRequestStatus.Cancelled,
            PaymentRequestLifecycleEventKind.Expired => PaymentRequestStatus.Expired,
            PaymentRequestLifecycleEventKind.Paid => PaymentRequestStatus.Paid,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported payment request lifecycle event kind.")
        };

        if (request.Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Lifecycle event {kind} cannot be emitted for payment request status {request.Status}.");
        }

        if (kind == PaymentRequestLifecycleEventKind.Created && occurredAtUtc != request.CreatedAtUtc)
        {
            throw new InvalidOperationException("Created event timestamp must equal payment request creation time.");
        }

        if (kind != PaymentRequestLifecycleEventKind.Created && occurredAtUtc != request.UpdatedAtUtc)
        {
            throw new InvalidOperationException("Lifecycle event timestamp must equal the payment request update time.");
        }

        return new PaymentRequestLifecycleEvent(
            Guid.NewGuid(),
            request.Id,
            kind,
            occurredAtUtc,
            request.Status,
            request.RequesterWalletId,
            request.Currency.Code,
            request.AmountMinor,
            request.ExpiresAtUtc,
            request.AcceptedPayerWalletId,
            request.TransferId);
    }

    public static PaymentRequestEventEnvelope ToEnvelope(PaymentRequestLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        if (lifecycleEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("Lifecycle event id cannot be empty.", nameof(lifecycleEvent));
        }

        if (lifecycleEvent.PaymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(lifecycleEvent));
        }

        if (lifecycleEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Lifecycle event timestamp must be UTC.", nameof(lifecycleEvent));
        }

        var eventType = lifecycleEvent.Kind switch
        {
            PaymentRequestLifecycleEventKind.Created => "payment-request.created",
            PaymentRequestLifecycleEventKind.Accepted => "payment-request.accepted",
            PaymentRequestLifecycleEventKind.Declined => "payment-request.declined",
            PaymentRequestLifecycleEventKind.Cancelled => "payment-request.cancelled",
            PaymentRequestLifecycleEventKind.Expired => "payment-request.expired",
            PaymentRequestLifecycleEventKind.Paid => "payment-request.paid",
            _ => throw new ArgumentOutOfRangeException(nameof(lifecycleEvent.Kind), lifecycleEvent.Kind, "Unsupported payment request lifecycle event kind.")
        };

        var payload = new PaymentRequestLifecycleEventPayload(
            1,
            lifecycleEvent.Status.ToString(),
            lifecycleEvent.RequesterWalletId.Value,
            lifecycleEvent.CurrencyCode,
            lifecycleEvent.AmountMinor,
            lifecycleEvent.ExpiresAtUtc,
            lifecycleEvent.AcceptedPayerWalletId?.Value,
            lifecycleEvent.TransferId);

        return new PaymentRequestEventEnvelope(
            lifecycleEvent.EventId,
            lifecycleEvent.PaymentRequestId,
            eventType,
            lifecycleEvent.OccurredAtUtc,
            JsonSerializer.Serialize(payload, JsonOptions));
    }

    private sealed record PaymentRequestLifecycleEventPayload(
        int Version,
        string Status,
        Guid RequesterWalletId,
        string CurrencyCode,
        long AmountMinor,
        DateTimeOffset? ExpiresAtUtc,
        Guid? AcceptedPayerWalletId,
        Guid? TransferId);
}
