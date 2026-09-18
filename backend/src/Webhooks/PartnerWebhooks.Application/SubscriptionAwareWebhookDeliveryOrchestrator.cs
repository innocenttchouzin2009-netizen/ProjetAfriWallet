namespace AfriWallet.PartnerWebhooks.Application;

public sealed class SubscriptionAwareWebhookDeliveryOrchestrator(
    PartnerWebhookSubscriptionApplicationService subscriptionService,
    ISubscriptionWebhookDeliveryPort deliveryPort)
{
    public async Task<SubscriptionWebhookDeliveryBatchResult> DeliverAsync(
        OutboundWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var subscriptions = await subscriptionService.ListActiveByEventTypeAsync(
            webhookEvent.EventType,
            cancellationToken);

        var ordered = subscriptions
            .OrderBy(subscription => subscription.Id.Value)
            .ToArray();

        var dispatched = 0;

        foreach (var subscription in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await deliveryPort.DeliverAsync(
                new SubscriptionWebhookDelivery(
                    subscription.Id,
                    subscription.PartnerId,
                    subscription.Endpoint,
                    subscription.SigningSecretReference,
                    webhookEvent),
                cancellationToken);

            dispatched++;
        }

        return new SubscriptionWebhookDeliveryBatchResult(
            ordered.Length,
            dispatched);
    }
}
