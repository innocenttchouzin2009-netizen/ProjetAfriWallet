namespace AfriWallet.PartnerWebhooks.Application;

public interface ISubscriptionWebhookDeliveryPort
{
    Task DeliverAsync(
        SubscriptionWebhookDelivery delivery,
        CancellationToken cancellationToken = default);
}
