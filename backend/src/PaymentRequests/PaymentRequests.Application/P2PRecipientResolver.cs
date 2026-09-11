using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class P2PRecipientResolver(RecipientResolutionService recipientResolutionService)
    : IPaymentRequestRecipientResolver
{
    public async Task<WalletId?> ResolveAsync(
        RecipientReference reference,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(currency);

        var result = await recipientResolutionService.ResolveAsync(
            new ResolveRecipientRequest(reference, currency),
            cancellationToken);

        return result.Status == RecipientResolutionStatus.Success
            ? result.Recipient?.WalletId
            : null;
    }
}
