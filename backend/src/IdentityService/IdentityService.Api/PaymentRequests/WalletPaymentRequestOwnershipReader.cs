using AfriWallet.PaymentRequests.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.PaymentRequests;

public sealed class WalletPaymentRequestOwnershipReader(IWalletRepository walletRepository)
    : IPaymentRequestWalletOwnershipReader
{
    public async Task<bool> IsOwnedByAsync(
        WalletId walletId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            return false;
        }

        var wallet = await walletRepository.GetAsync(walletId, cancellationToken);
        return wallet is not null && wallet.OwnerId == ownerId;
    }
}
