using System.Text.Json;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestNotificationProjectionResult(
    bool Ignored,
    int ProjectedCount,
    int DuplicateCount)
{
    public static PaymentRequestNotificationProjectionResult IgnoredEvent() => new(true, 0, 0);
}

public sealed class PaymentRequestNotificationProjectionEngine(
    IPaymentRequestNotificationAudienceResolver audienceResolver,
    IPaymentRequestNotificationProjectionStore projectionStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PaymentRequestNotificationProjectionResult> ProjectAsync(
        PaymentRequestEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        if (envelope.EventId == Guid.Empty)
        {
            throw new ArgumentException("Lifecycle event id cannot be empty.", nameof(envelope));
        }

        if (envelope.PaymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(envelope));
        }

        if (envelope.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Lifecycle event timestamp must be UTC.", nameof(envelope));
        }

        if (!PaymentRequestNotificationEventTypes.TryGetKind(envelope.EventType, out var kind))
        {
            return PaymentRequestNotificationProjectionResult.IgnoredEvent();
        }

        if (string.IsNullOrWhiteSpace(envelope.PayloadJson))
        {
            throw new ArgumentException("Lifecycle event payload is required.", nameof(envelope));
        }

        LifecyclePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<LifecyclePayload>(envelope.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Lifecycle event payload is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Lifecycle event payload is invalid JSON.", exception);
        }

        if (payload.Version != 1)
        {
            throw new InvalidOperationException($"Unsupported lifecycle event payload version {payload.Version}.");
        }

        if (!Enum.TryParse<PaymentRequestStatus>(payload.Status, ignoreCase: false, out var status) || !Enum.IsDefined(status))
        {
            throw new InvalidOperationException("Lifecycle event payload contains an invalid payment request status.");
        }

        var source = new PaymentRequestNotificationProjectionSource(
            envelope.EventId,
            envelope.PaymentRequestId,
            envelope.EventType,
            envelope.OccurredAtUtc,
            status,
            WalletId.From(payload.RequesterWalletId),
            Currency.Create(payload.CurrencyCode),
            payload.AmountMinor,
            payload.ExpiresAtUtc,
            payload.AcceptedPayerWalletId is null ? null : WalletId.From(payload.AcceptedPayerWalletId.Value),
            payload.TransferId);

        var recipients = await audienceResolver.ResolveAsync(source, cancellationToken);
        var seen = new HashSet<PaymentRequestNotificationProjectionKey>();
        var projected = 0;
        var duplicates = 0;

        foreach (var recipient in recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = new PaymentRequestNotificationProjectionKey(
                source.SourceEventId,
                recipient.OwnerId,
                recipient.Audience);

            if (!seen.Add(key))
            {
                continue;
            }

            var notification = PaymentRequestNotification.Project(
                source.SourceEventId,
                source.PaymentRequestId,
                kind,
                recipient.Audience,
                recipient.OwnerId,
                source.RequesterWalletId,
                source.Currency,
                source.AmountMinor,
                source.OccurredAtUtc,
                source.Status,
                source.ExpiresAtUtc,
                source.TransferId);

            if (await projectionStore.TryAddAsync(notification, cancellationToken))
            {
                projected++;
            }
            else
            {
                duplicates++;
            }
        }

        return new PaymentRequestNotificationProjectionResult(false, projected, duplicates);
    }

    private sealed record LifecyclePayload(
        int Version,
        string Status,
        Guid RequesterWalletId,
        string CurrencyCode,
        long AmountMinor,
        DateTimeOffset? ExpiresAtUtc,
        Guid? AcceptedPayerWalletId,
        Guid? TransferId);
}
