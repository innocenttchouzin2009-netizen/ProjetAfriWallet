using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Infrastructure.Intents;

public sealed class PaymentWalletReader : IPaymentWalletReader
{
    private readonly IWalletRepository _walletRepository;

    public PaymentWalletReader(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public Task<PaymentWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken)
    {
        var wallet = _walletRepository.GetById(walletId);
        return Task.FromResult(wallet is null ? null : new PaymentWalletSnapshot(wallet.Id, wallet.AwidId, wallet.Currency, wallet.Status));
    }
}
