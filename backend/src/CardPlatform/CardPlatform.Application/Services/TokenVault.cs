namespace AfriWallet.CardPlatform.Application.Services;

public sealed class TokenVault
{
    public string CreateReference()
    {
        return $"tok_{Guid.NewGuid().ToString("N")[..12]}";
    }
}
