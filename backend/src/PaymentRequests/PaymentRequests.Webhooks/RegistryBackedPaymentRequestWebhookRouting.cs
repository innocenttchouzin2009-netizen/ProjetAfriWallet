using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed record PaymentRequestWebhookSubscription(
    Guid SubscriptionId,
    RecipientReference Recipient,
    Uri Endpoint,
    bool IsActive,
    IReadOnlySet<string> AuthorizedEventTypes,
    PaymentRequestWebhookRetryPolicy RetryPolicy,
    string SigningConfigurationId);

public sealed class ConfiguredPaymentRequestWebhookSubscriptionRegistry(
    IEnumerable<PaymentRequestWebhookSubscription> subscriptions)
    : IPaymentRequestWebhookDestinationRegistry
{
    private readonly IReadOnlyList<PaymentRequestWebhookSubscription> values =
        subscriptions?.ToArray() ?? throw new ArgumentNullException(nameof(subscriptions));

    public Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveAuthorizedAsync(
        RecipientReference recipient,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));

        IReadOnlyList<PaymentRequestWebhookDestination> result = values
            .Where(subscription =>
                subscription.IsActive &&
                subscription.Recipient == recipient &&
                subscription.AuthorizedEventTypes.Contains(eventType))
            .Select(subscription => new PaymentRequestWebhookDestination(
                subscription.SubscriptionId,
                subscription.Endpoint,
                subscription.RetryPolicy,
                subscription.SigningConfigurationId))
            .ToArray();

        return Task.FromResult(result);
    }
}

public sealed record PaymentRequestWebhookSigningConfiguration(
    string ConfigurationId,
    PaymentRequestWebhookSecret Secret);

public sealed class ConfiguredPaymentRequestWebhookSignerRegistry(
    IEnumerable<PaymentRequestWebhookSigningConfiguration> configurations)
    : IPaymentRequestWebhookSignerRegistry
{
    private readonly IReadOnlyDictionary<string, PaymentRequestWebhookSigningConfiguration> values =
        (configurations ?? throw new ArgumentNullException(nameof(configurations)))
        .ToDictionary(item => item.ConfigurationId, StringComparer.Ordinal);

    public Task<IPaymentRequestWebhookSigner> ResolveAsync(
        Guid destinationId,
        string signingConfigurationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (destinationId == Guid.Empty)
            throw new ArgumentException("Destination id cannot be empty.", nameof(destinationId));
        if (string.IsNullOrWhiteSpace(signingConfigurationId))
            throw new ArgumentException("Signing configuration id is required.", nameof(signingConfigurationId));
        if (!values.TryGetValue(signingConfigurationId, out var configuration))
            throw new InvalidOperationException("Authorized destination signing configuration was not found.");

        IPaymentRequestWebhookSigner signer = new HmacPaymentRequestWebhookSignerAdapter(
            new PaymentRequestWebhookSigner(
                new RotatingPaymentRequestWebhookSecretProvider(configuration.Secret)));

        return Task.FromResult(signer);
    }
}

internal sealed class HmacPaymentRequestWebhookSignerAdapter(PaymentRequestWebhookSigner signer)
    : IPaymentRequestWebhookSigner
{
    public Task<PaymentRequestWebhookSignature> SignAsync(
        PaymentRequestWebhookSignatureInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = System.Text.Encoding.UTF8.GetBytes(input.PayloadJson);
        var headers = signer.Sign(input.EventId, payload, input.AttemptedAtUtc);
        return Task.FromResult(new PaymentRequestWebhookSignature(
            "hmac-sha256",
            headers.Signature,
            headers.KeyId));
    }
}
