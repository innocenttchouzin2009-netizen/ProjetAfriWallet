using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Push.Infrastructure;

public sealed class ProviderBackedPushNotificationTransport(
    IDevicePushRegistrationRepository registrationRepository,
    IEnumerable<IPushProviderClient> providerClients)
    : IPushNotificationTransport
{
    private readonly IReadOnlyList<IPushProviderClient> clients = providerClients.ToArray();

    public async Task<PushDeliveryResult> SendAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var registrations = await registrationRepository.ListActiveByUserAsync(
            message.RecipientUserId,
            cancellationToken);

        if (registrations.Count == 0)
        {
            return PushDeliveryResult.Permanent("no-active-push-device");
        }

        PushDeliveryResult? firstDelivered = null;
        PushDeliveryResult? firstRetryable = null;
        PushDeliveryResult? firstPermanent = null;

        foreach (var registration in registrations.OrderByDescending(value => value.LastSeenAtUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var client = clients.SingleOrDefault(value => value.Supports(registration.Platform));
            if (client is null)
            {
                firstPermanent ??= PushDeliveryResult.Permanent("push-provider-unavailable");
                continue;
            }

            var result = await client.SendAsync(registration, message, cancellationToken);
            switch (result.Disposition)
            {
                case PushDeliveryDisposition.Delivered:
                    firstDelivered ??= result;
                    break;
                case PushDeliveryDisposition.RetryableFailure:
                    firstRetryable ??= result;
                    break;
                case PushDeliveryDisposition.PermanentFailure:
                    firstPermanent ??= result;
                    break;
                default:
                    throw new InvalidOperationException("Unknown push delivery disposition.");
            }
        }

        // The application contract is intentionally aggregate. Once at least one active
        // device receives the notification, the user-level delivery is considered successful
        // so retries do not duplicate notifications on already-delivered devices.
        if (firstDelivered is not null)
        {
            return firstDelivered;
        }

        return firstRetryable ?? firstPermanent ?? PushDeliveryResult.Permanent("push-delivery-failed");
    }
}
