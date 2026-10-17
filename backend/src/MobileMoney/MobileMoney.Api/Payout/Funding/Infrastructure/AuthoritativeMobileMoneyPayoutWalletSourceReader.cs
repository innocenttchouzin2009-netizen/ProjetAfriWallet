using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Infrastructure;

public sealed class AuthoritativeMobileMoneyPayoutWalletSourceReader(
    IWalletRepository walletRepository,
    IMobileWalletBalanceReader balanceReader)
    : IMobileMoneyPayoutAuthoritativeWalletSourceReader
{
    public async Task<FundingSourceSnapshot?> GetAsync(
        Guid ownerUserId,
        string sourceId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));
        if (string.IsNullOrWhiteSpace(sourceId))
            return null;

        var normalizedSourceId = sourceId.Trim();
        if (!Guid.TryParseExact(normalizedSourceId, "D", out var walletGuid)
            || walletGuid == Guid.Empty
            || !string.Equals(
                normalizedSourceId,
                walletGuid.ToString("D"),
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedCurrency = FundingAllocation.NormalizeCurrency(currencyCode);
        var wallet = await walletRepository.GetAsync(
            WalletId.From(walletGuid),
            cancellationToken);

        if (wallet is null
            || wallet.OwnerId != ownerUserId
            || wallet.Status != WalletStatus.Active
            || !string.Equals(
                wallet.Currency.Code,
                normalizedCurrency,
                StringComparison.Ordinal))
        {
            return null;
        }

        var availableMinor = await balanceReader.ReadAvailableMinorAsync(
            walletGuid,
            normalizedCurrency,
            cancellationToken);

        if (availableMinor < 0)
            throw new InvalidOperationException(
                "Authoritative wallet available balance cannot be negative.");

        return new FundingSourceSnapshot(
            walletGuid.ToString("D"),
            FundingSourceType.Wallet,
            normalizedCurrency,
            true,
            availableMinor);
    }
}
