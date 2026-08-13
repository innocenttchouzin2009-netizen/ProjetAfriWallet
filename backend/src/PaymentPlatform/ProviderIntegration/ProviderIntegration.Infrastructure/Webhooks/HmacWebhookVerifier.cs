using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentPlatform.ProviderIntegration.Application;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Webhooks;

public sealed class HmacWebhookVerifier : IProviderWebhookVerifier
{
    private readonly IProviderSecretSource _secrets;

    public HmacWebhookVerifier(IProviderSecretSource secrets)
    {
        _secrets = secrets;
    }

    public bool Verify(ProviderWebhookVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ProviderCode) ||
            request.ProviderCode.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("Provider code must be alphanumeric.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
            return false;

        var secretKey =
            $"AFW_PROVIDER_{request.ProviderCode.ToUpperInvariant()}_WEBHOOK_SECRET";
        var secret = _secrets.GetRequired(secretKey);
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        try
        {
            using var hmac = new HMACSHA256(secretBytes);
            var calculated = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Payload));

            byte[] expected;

            try
            {
                expected = Convert.FromHexString(request.Signature.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(calculated, expected);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }
}