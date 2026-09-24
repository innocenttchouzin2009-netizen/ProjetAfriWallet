using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;

namespace IdentityService.Api.Wallet;

public sealed class WalletBalanceReadApplicationService(
    WalletRegistryApplicationService walletRegistry,
    IWalletLedgerAccountResolver accountResolver,
    LedgerBackedBalanceReadService balanceReadService)
{
    public async Task<WalletOperationResult<IReadOnlyList<WalletBalanceReadDto>>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var walletsResult = await walletRegistry.ListByOwnerAsync(ownerId, cancellationToken);
        if (!walletsResult.Succeeded || walletsResult.Value is null)
        {
            return WalletOperationResult<IReadOnlyList<WalletBalanceReadDto>>.Failure(
                walletsResult.ErrorCode ?? WalletErrorCode.ValidationError,
                walletsResult.ErrorMessage ?? "Wallet balance read failed.");
        }

        var balances = new List<WalletBalanceReadDto>(walletsResult.Value.Count);

        foreach (var wallet in walletsResult.Value)
        {
            var accountId = await accountResolver.ResolveAsync(wallet.WalletId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Wallet '{wallet.WalletId}' has no resolved Ledger account.");

            var snapshot = await balanceReadService.ReadAsync(
                new BalanceKey(accountId, wallet.CurrencyCode),
                cancellationToken);

            balances.Add(new WalletBalanceReadDto(
                wallet.WalletId,
                wallet.CurrencyCode,
                snapshot.NetMinor,
                wallet.Status.ToString().ToUpperInvariant(),
                wallet.CountryCode));
        }

        return WalletOperationResult<IReadOnlyList<WalletBalanceReadDto>>.Success(balances);
    }
}
