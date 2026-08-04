using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Infrastructure.Intents;

public sealed class PaymentRecipientResolver : IPaymentRecipientResolver
{
    private readonly IWalletRepository _walletRepository;

    public PaymentRecipientResolver(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public Task<ResolvedRecipient?> ResolveAsync(RecipientType type, string reference, CancellationToken cancellationToken)
    {
        if (type == RecipientType.Awid)
        {
            return Task.FromResult<ResolvedRecipient?>(new ResolvedRecipient(null, reference));
        }

        if (type == RecipientType.Wallet && Guid.TryParse(reference, out var walletId))
        {
            var wallet = _walletRepository.GetById(walletId);
            return Task.FromResult<ResolvedRecipient?>(wallet is null ? null : new ResolvedRecipient(wallet.Id, wallet.WalletNumber));
        }

        return Task.FromResult<ResolvedRecipient?>(null);
    }
}
