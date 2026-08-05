using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class TokenValidator
{
    public bool IsValid(CardToken token)
    {
        return !string.IsNullOrWhiteSpace(token.TokenReference)
            && token.Status is "ACTIVE" or "ROTATED";
    }
}
