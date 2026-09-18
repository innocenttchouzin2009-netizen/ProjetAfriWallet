namespace AfriWallet.PaymentRequests.Webhooks;

public sealed record PaymentRequestWebhookSecret(
    string KeyId,
    string Secret,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset? NotAfterUtc = null)
{
    public bool IsActiveAt(DateTimeOffset atUtc)
    {
        if (atUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Secret evaluation time must be UTC.", nameof(atUtc));

        return atUtc >= NotBeforeUtc && (NotAfterUtc is null || atUtc < NotAfterUtc.Value);
    }
}

public interface IPaymentRequestWebhookSecretProvider
{
    PaymentRequestWebhookSecret GetSigningSecret(DateTimeOffset nowUtc);
}

public sealed class RotatingPaymentRequestWebhookSecretProvider : IPaymentRequestWebhookSecretProvider
{
    private readonly PaymentRequestWebhookSecret current;

    public RotatingPaymentRequestWebhookSecretProvider(PaymentRequestWebhookSecret current)
    {
        ArgumentNullException.ThrowIfNull(current);
        ValidateSecret(current);
        this.current = current;
    }

    public PaymentRequestWebhookSecret GetSigningSecret(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (!current.IsActiveAt(nowUtc))
            throw new InvalidOperationException("Current webhook signing key is not active.");

        return current;
    }

    internal static bool IsValidKeyId(string? keyId) =>
        !string.IsNullOrWhiteSpace(keyId) &&
        keyId.Length <= 64 &&
        keyId.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '.' or '_' or '-');

    private static void ValidateSecret(PaymentRequestWebhookSecret secret)
    {
        if (!IsValidKeyId(secret.KeyId))
            throw new ArgumentException("Webhook key id must contain only ASCII letters, digits, '.', '_' or '-' and be at most 64 characters.");

        if (string.IsNullOrWhiteSpace(secret.Secret) || secret.Secret.Length < 32)
            throw new ArgumentException("Webhook secret must contain at least 32 characters.");

        EnsureUtc(secret.NotBeforeUtc, nameof(secret.NotBeforeUtc));
        if (secret.NotAfterUtc is not null)
        {
            EnsureUtc(secret.NotAfterUtc.Value, nameof(secret.NotAfterUtc));
            if (secret.NotAfterUtc <= secret.NotBeforeUtc)
                throw new ArgumentException("Webhook secret expiration must be later than activation.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
