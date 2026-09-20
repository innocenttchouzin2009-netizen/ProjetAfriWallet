using System.Text.RegularExpressions;

namespace AfriWallet.Webhooks.Operations.Domain;

public readonly record struct WebhookSigningSecretReference
{
    private static readonly Regex Allowed = new(
        "^[A-Za-z0-9][A-Za-z0-9._:/-]{0,255}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public WebhookSigningSecretReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Webhook signing secret reference is required.", nameof(value));

        var normalized = value.Trim();
        if (!Allowed.IsMatch(normalized))
            throw new ArgumentException("Webhook signing secret reference has an invalid format.", nameof(value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct RetryPolicyReference
{
    public RetryPolicyReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Retry policy reference is required.", nameof(value));

        var normalized = value.Trim();
        if (normalized.Length > 128)
            throw new ArgumentException("Retry policy reference must be at most 128 characters.", nameof(value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record WebhookDeliveryConfiguration
{
    public WebhookDeliveryConfiguration(
        TimeSpan timeout,
        RetryPolicyReference retryPolicy,
        int maxInFlight = 1)
    {
        if (timeout < TimeSpan.FromSeconds(1) || timeout > TimeSpan.FromSeconds(60))
            throw new ArgumentOutOfRangeException(nameof(timeout), "Webhook timeout must be between 1 and 60 seconds.");
        if (maxInFlight is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(maxInFlight), "Webhook max in-flight deliveries must be between 1 and 32.");

        Timeout = timeout;
        RetryPolicy = retryPolicy;
        MaxInFlight = maxInFlight;
    }

    public TimeSpan Timeout { get; }
    public RetryPolicyReference RetryPolicy { get; }
    public int MaxInFlight { get; }
}
