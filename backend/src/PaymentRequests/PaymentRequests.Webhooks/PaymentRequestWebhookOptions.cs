namespace AfriWallet.PaymentRequests.Webhooks;

public sealed record PaymentRequestWebhookSecurityOptions(
    TimeSpan MaximumClockSkew,
    TimeSpan ReplayWindow)
{
    public static PaymentRequestWebhookSecurityOptions Default { get; } =
        new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));

    public void Validate()
    {
        if (MaximumClockSkew <= TimeSpan.Zero || MaximumClockSkew > TimeSpan.FromHours(1))
            throw new InvalidOperationException("Webhook maximum clock skew must be greater than zero and no more than one hour.");

        if (ReplayWindow < MaximumClockSkew + MaximumClockSkew)
            throw new InvalidOperationException("Webhook replay window must be at least twice the maximum clock skew.");
    }
}
