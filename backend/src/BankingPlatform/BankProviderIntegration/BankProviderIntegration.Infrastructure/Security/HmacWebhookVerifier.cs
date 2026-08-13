using System.Security.Cryptography;
using System.Text;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Security;

public sealed class HmacWebhookVerifier : IWebhookVerifier
{
    private readonly IRequestSigner _signer;

    public HmacWebhookVerifier(IRequestSigner signer)
    {
        _signer = signer;
    }

    public bool Verify(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = _signer.Sign(payload, secret);

        try
        {
            var expectedBytes = Convert.FromHexString(expected);
            var actualBytes = Convert.FromHexString(signature);

            return expectedBytes.Length == actualBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       expectedBytes,
                       actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
