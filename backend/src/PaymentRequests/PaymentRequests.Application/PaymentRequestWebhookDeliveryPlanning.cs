using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestWebhookRetryPolicy(
    int MaxAttempts,
    TimeSpan BaseDelay,
    TimeSpan MaxDelay)
{
    public IReadOnlyList<TimeSpan> BuildRetryDelays()
    {
        if (MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "Max attempts must be at least one.");
        }

        if (BaseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(BaseDelay), "Base retry delay must be positive.");
        }

        if (MaxDelay < BaseDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDelay), "Max retry delay cannot be less than base retry delay.");
        }

        var delays = new List<TimeSpan>(Math.Max(0, MaxAttempts - 1));
        var current = BaseDelay;

        for (var retryNumber = 1; retryNumber < MaxAttempts; retryNumber++)
        {
            delays.Add(current);

            if (current >= MaxDelay)
            {
                current = MaxDelay;
                continue;
            }

            var doubledTicks = current.Ticks > long.MaxValue / 2
                ? long.MaxValue
                : current.Ticks * 2;
            current = TimeSpan.FromTicks(Math.Min(doubledTicks, MaxDelay.Ticks));
        }

        return delays;
    }
}

public sealed record PaymentRequestWebhookDestination(
    Guid DestinationId,
    Uri Endpoint,
    PaymentRequestWebhookRetryPolicy RetryPolicy);

public interface IPaymentRequestWebhookDestinationRegistry
{
    Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveAuthorizedAsync(
        RecipientReference recipient,
        string eventType,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestWebhookDeliveryPlanItem(
    Guid DestinationId,
    Uri Endpoint,
    int MaxAttempts,
    IReadOnlyList<TimeSpan> RetryDelays);

public sealed record PaymentRequestWebhookDeliveryPlan(
    Guid EventId,
    PaymentRequestId PaymentRequestId,
    string EventType,
    IReadOnlyList<PaymentRequestWebhookDeliveryPlanItem> Destinations);

public enum PaymentRequestWebhookDeliveryPlanningStatus
{
    Planned = 1,
    RequestNotFound = 2,
    NoAuthorizedDestinations = 3
}

public sealed record PaymentRequestWebhookDeliveryPlanningResult(
    PaymentRequestWebhookDeliveryPlanningStatus Status,
    PaymentRequestWebhookDeliveryPlan? Plan)
{
    public static PaymentRequestWebhookDeliveryPlanningResult Planned(PaymentRequestWebhookDeliveryPlan plan) =>
        new(PaymentRequestWebhookDeliveryPlanningStatus.Planned, plan);

    public static PaymentRequestWebhookDeliveryPlanningResult RequestNotFound() =>
        new(PaymentRequestWebhookDeliveryPlanningStatus.RequestNotFound, null);

    public static PaymentRequestWebhookDeliveryPlanningResult NoAuthorizedDestinations() =>
        new(PaymentRequestWebhookDeliveryPlanningStatus.NoAuthorizedDestinations, null);
}

public sealed class PaymentRequestWebhookDeliveryPlanner(
    IPaymentRequestRepository paymentRequestRepository,
    IPaymentRequestWebhookDestinationRegistry destinationRegistry)
{
    public async Task<PaymentRequestWebhookDeliveryPlanningResult> PlanAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEvent(paymentRequestEvent);

        var request = await paymentRequestRepository.GetAsync(
            paymentRequestEvent.PaymentRequestId,
            cancellationToken);

        if (request is null)
        {
            return PaymentRequestWebhookDeliveryPlanningResult.RequestNotFound();
        }

        var destinations = await destinationRegistry.ResolveAuthorizedAsync(
            request.PayerReference,
            paymentRequestEvent.EventType,
            cancellationToken);

        if (destinations is null || destinations.Count == 0)
        {
            return PaymentRequestWebhookDeliveryPlanningResult.NoAuthorizedDestinations();
        }

        var seenDestinationIds = new HashSet<Guid>();
        var planItems = new List<PaymentRequestWebhookDeliveryPlanItem>(destinations.Count);

        foreach (var destination in destinations)
        {
            ValidateDestination(destination);

            if (!seenDestinationIds.Add(destination.DestinationId))
            {
                throw new InvalidOperationException("Destination registry returned a duplicate destination id.");
            }

            var retryDelays = destination.RetryPolicy.BuildRetryDelays();
            planItems.Add(new PaymentRequestWebhookDeliveryPlanItem(
                destination.DestinationId,
                destination.Endpoint,
                destination.RetryPolicy.MaxAttempts,
                retryDelays));
        }

        return PaymentRequestWebhookDeliveryPlanningResult.Planned(
            new PaymentRequestWebhookDeliveryPlan(
                paymentRequestEvent.EventId,
                paymentRequestEvent.PaymentRequestId,
                paymentRequestEvent.EventType,
                planItems));
    }

    private static void ValidateEvent(PaymentRequestEventEnvelope paymentRequestEvent)
    {
        if (paymentRequestEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.PaymentRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestEvent));
        }

        if (string.IsNullOrWhiteSpace(paymentRequestEvent.EventType))
        {
            throw new ArgumentException("Event type is required.", nameof(paymentRequestEvent));
        }

        if (paymentRequestEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamp must be UTC.", nameof(paymentRequestEvent));
        }
    }

    private static void ValidateDestination(PaymentRequestWebhookDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(destination.Endpoint);
        ArgumentNullException.ThrowIfNull(destination.RetryPolicy);

        if (destination.DestinationId == Guid.Empty)
        {
            throw new InvalidOperationException("Destination registry returned an empty destination id.");
        }

        if (!destination.Endpoint.IsAbsoluteUri ||
            (destination.Endpoint.Scheme != Uri.UriSchemeHttps &&
             destination.Endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException("Destination registry returned an invalid webhook endpoint.");
        }
    }
}
