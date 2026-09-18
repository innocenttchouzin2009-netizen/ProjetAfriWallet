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

    PaymentRequestWebhookSecret? GetVerificationSecret(
        string keyId,
        DateTimeOffset signedAtUtc,
        DateTimeOffset nowUtc);
}

public sealed class RotatingPaymentRequestWebhookSecretProvider : IPaymentRequestWebhookSecretProvider
{
    private readonly PaymentRequestWebhookSecret current;
    private readonly IReadOnlyDictionary<string, PaymentRequestWebhookSecret> verificationKeys;

    public RotatingPaymentRequestWebhookSecretProvider(
        PaymentRequestWebhookSecret current,
        IEnumerable<PaymentRequestWebhookSecret>? previous = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        ValidateSecret(current);

        var values = new List<PaymentRequestWebhookSecret> { current };
        if (previous is not null)
        {
            foreach (var secret in previous)
            {
                ArgumentNullException.ThrowIfNull(secret);
                ValidateSecret(secret);
                values.Add(secret);
            }
        }

        if (values.Select(x => x.KeyId).Distinct(StringComparer.Ordinal).Count() != values.Count)
            throw new ArgumentException("Webhook key ids must be unique.");

        this.current = current;
        verificationKeys = values.ToDictionary(x => x.KeyId, StringComparer.Ordinal);
    }

    public PaymentRequestWebhookSecret GetSigningSecret(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (!current.IsActiveAt(nowUtc))
            throw new InvalidOperationException("Current webhook signing key is not active.");

        return current;
    }

    public PaymentRequestWebhookSecret? GetVerificationSecret(
        string keyId,
        DateTimeOffset signedAtUtc,
        DateTimeOffset nowUtc)
    {
        EnsureUtc(signedAtUtc, nameof(signedAtUtc));
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (!IsValidKeyId(keyId))
            return null;

        return verificationKeys.TryGetValue(keyId, out var secret) &&
               secret.IsActiveAt(signedAtUtc) &&
               secret.IsActiveAt(nowUtc)
            ? secret
            : null;
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
