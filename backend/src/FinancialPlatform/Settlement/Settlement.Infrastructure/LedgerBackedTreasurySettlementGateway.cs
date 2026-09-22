using System.Security.Cryptography;
using System.Text;
using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using Settlement.Application.Interfaces;

namespace Settlement.Infrastructure.Gateways;

public sealed class LedgerBackedTreasurySettlementGateway(
    LedgerPostingApplicationService ledgerPosting,
    LedgerBackedBalanceReadService balanceReader,
    ISettlementFxClearingAccountResolver fxClearingAccounts,
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

        if (string.Equals(posting.SourceCurrency, posting.DestinationCurrency, StringComparison.OrdinalIgnoreCase))
        {
            await PostSameCurrencyAsync(posting, cancellationToken);
            return;
        }

        await PostCrossCurrencyAsync(posting, cancellationToken);
    }

    private async Task PostSameCurrencyAsync(
        TreasurySettlementPosting posting,
        CancellationToken cancellationToken)
    {
        var existing = await ledgerPosting.GetByCorrelationIdAsync(posting.InstructionId, cancellationToken);
        if (existing.Succeeded) return;

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

    private async Task PostCrossCurrencyAsync(
        TreasurySettlementPosting posting,
        CancellationToken cancellationToken)
    {
        var clearing = fxClearingAccounts.Resolve(posting.SourceCurrency, posting.DestinationCurrency);
        var sourceCorrelation = DeriveCorrelation(posting.InstructionId, "fx-source");
        var destinationCorrelation = DeriveCorrelation(posting.InstructionId, "fx-destination");

        var sourceExisting = await ledgerPosting.GetByCorrelationIdAsync(sourceCorrelation, cancellationToken);
        if (!sourceExisting.Succeeded)
        {
            var sourceResult = await ledgerPosting.PostAsync(
                new PostJournalCommand(
                    posting.SourceCurrency,
                    $"settlement-fx-source:{posting.InstructionId:D}",
                    sourceCorrelation,
                    [
                        new PostLedgerLineCommand(posting.SourceAccountId, LedgerSide.Debit, posting.SourceAmountMinor, "FX settlement source"),
                        new PostLedgerLineCommand(clearing.SourceClearingAccountId, LedgerSide.Credit, posting.SourceAmountMinor, "FX source clearing")
                    ]),
                timeProvider.GetUtcNow(),
                cancellationToken);

            if (!sourceResult.Succeeded && sourceResult.ErrorCode != LedgerErrorCode.DuplicateCorrelation)
                throw new InvalidOperationException(sourceResult.ErrorMessage ?? "Source-currency FX clearing posting failed.");
        }

        var destinationExisting = await ledgerPosting.GetByCorrelationIdAsync(destinationCorrelation, cancellationToken);
        if (!destinationExisting.Succeeded)
        {
            var destinationResult = await ledgerPosting.PostAsync(
                new PostJournalCommand(
                    posting.DestinationCurrency,
                    $"settlement-fx-destination:{posting.InstructionId:D}",
                    destinationCorrelation,
                    [
                        new PostLedgerLineCommand(clearing.DestinationClearingAccountId, LedgerSide.Debit, posting.DestinationAmountMinor, "FX destination clearing"),
                        new PostLedgerLineCommand(posting.DestinationAccountId, LedgerSide.Credit, posting.DestinationAmountMinor, "FX settlement destination")
                    ]),
                timeProvider.GetUtcNow(),
                cancellationToken);

            if (!destinationResult.Succeeded && destinationResult.ErrorCode != LedgerErrorCode.DuplicateCorrelation)
                throw new InvalidOperationException(destinationResult.ErrorMessage ?? "Destination-currency FX clearing posting failed.");
        }
    }

    private static Guid DeriveCorrelation(Guid instructionId, string suffix)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{instructionId:D}:{suffix}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
