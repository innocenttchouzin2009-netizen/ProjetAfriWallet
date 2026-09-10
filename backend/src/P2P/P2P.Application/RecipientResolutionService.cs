using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Application;

public sealed class RecipientResolutionService(
    IAfWalIdRecipientLookup afWalIdLookup,
    IQrRecipientLookup qrLookup)
{
    public async Task<RecipientResolutionResult> ResolveAsync(
        ResolveRecipientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Reference);
        ArgumentNullException.ThrowIfNull(request.Currency);

        cancellationToken.ThrowIfCancellationRequested();

        WalletId? walletId = request.Reference.Kind switch
        {
            RecipientReferenceKind.AfWalId => await afWalIdLookup.ResolveAsync(
                request.Reference.Value,
                request.Currency,
                cancellationToken),
            RecipientReferenceKind.QrToken => await qrLookup.ResolveAsync(
                request.Reference.Value,
                request.Currency,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Reference.Kind))
        };

        if (walletId is null)
        {
            return RecipientResolutionResult.NotFound();
        }

        return RecipientResolutionResult.Found(new ResolvedRecipient(
            walletId.Value,
            request.Currency,
            request.Reference));
    }
}
