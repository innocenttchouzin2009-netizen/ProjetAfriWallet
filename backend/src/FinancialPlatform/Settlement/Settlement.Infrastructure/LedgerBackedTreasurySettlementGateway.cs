using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using Settlement.Application.Interfaces;

namespace Settlement.Infrastructure.Gateways;

public sealed class LedgerBackedTreasurySettlementGateway(
    LedgerPostingApplicationService ledgerPosting,
    LedgerBackedBalanceReadService balanceReader,
    TimeProvider timeProvider)
    : ITreasurySettlementGateway
{
    public async Task<bool> HasAvailableFundsAsync(
        Guid accountId,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty) return false;
        if (amountMinor <= 0) return false;

        var snapshot = await balanceReader.ReadAsync(
            new BalanceKey(new AccountId(accountId), currencyCode),
            cancellationToken);

        return snapshot.NetMinor >= amountMinor;
    }

    public async Task PostSettlementAsync(TreasurySettlementPosting posting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(posting);

        if (!string.Equals(posting.SourceCurrency, posting.DestinationCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cross-currency ledger posting requires an FX clearing model and is outside AFW-BE-SETTLEMENT-1.");

        var existing = await ledgerPosting.GetByCorrelationIdAsync(posting.InstructionId, cancellationToken);
        if (existing.Succeeded)
            return;

        var result = await ledgerPosting.PostAsync(
            new PostJournalCommand(
                posting.SourceCurrency,
                $"settlement:{posting.InstructionId:D}",
                posting.InstructionId,
                [
                    new PostLedgerLineCommand(posting.SourceAccountId, LedgerSide.Debit, posting.SourceAmountMinor, "Settlement source"),
                    new PostLedgerLineCommand(posting.DestinationAccountId, LedgerSide.Credit, posting.DestinationAmountMinor, "Settlement destination")
                ]),
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (!result.Succeeded && result.ErrorCode != LedgerErrorCode.DuplicateCorrelation)
            throw new InvalidOperationException(result.ErrorMessage ?? "Settlement ledger posting failed.");
    }
}
