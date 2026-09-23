using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using AfriWallet.Merchants.Receivables.Application;
using AfriWallet.Transfer.Application;

namespace AfriWallet.Merchants.Payout.Infrastructure;

public sealed class LedgerBackedAfWalWalletPayoutFinalizer(
    IMerchantReceivableRepository receivables,
    IWalletLedgerAccountResolver walletLedgerAccounts,
    LedgerPostingApplicationService ledger,
    AccountId payoutClearingAccount)
    : IMerchantPayoutFinalizer
{
    public async Task<MerchantPayoutFinalizationResult> FinalizeAsync(
        MerchantPayoutFinalizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.DestinationType != MerchantPayoutDestinationType.AfWalWallet)
            return MerchantPayoutFinalizationResult.None;

        if (!Guid.TryParse(request.DestinationReference, out var walletId) || walletId == Guid.Empty)
            throw new InvalidOperationException("AfWal wallet payout destination reference must be a wallet GUID.");

        var destinationAccount = await walletLedgerAccounts.ResolveAsync(walletId, cancellationToken)
            ?? throw new InvalidOperationException("AfWal wallet payout destination has no Ledger account mapping.");

        var receivable = await receivables.GetAsync(request.ReceivableId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant receivable not found for payout finalization.");

        if (!string.Equals(receivable.MerchantId, request.MerchantId.Trim().ToUpperInvariant(), StringComparison.Ordinal))
            throw new InvalidOperationException("Payout merchant does not match the receivable.");
        if (!string.Equals(receivable.Currency, request.Currency.Trim().ToUpperInvariant(), StringComparison.Ordinal))
            throw new InvalidOperationException("Payout currency does not match the receivable.");
        if (receivable.NetAmountMinor != request.AmountMinor)
            throw new InvalidOperationException("Payout amount must equal the receivable net amount.");

        var existing = await ledger.GetByCorrelationIdAsync(request.PayoutId, cancellationToken);
        if (existing.Succeeded)
        {
            ValidateExisting(existing.Value!, request, destinationAccount);
        }
        else
        {
            var posted = await ledger.PostAsync(
                new PostJournalCommand(
                    request.Currency,
                    $"merchant-payout:{request.PayoutId:D}",
                    request.PayoutId,
                    [
                        new PostLedgerLineCommand(
                            payoutClearingAccount.Value,
                            LedgerSide.Debit,
                            request.AmountMinor,
                            "Merchant payout clearing"),
                        new PostLedgerLineCommand(
                            destinationAccount.Value,
                            LedgerSide.Credit,
                            request.AmountMinor,
                            "Merchant AfWal wallet payout")
                    ]),
                request.CompletedAtUtc,
                cancellationToken);

            if (!posted.Succeeded)
            {
                if (posted.ErrorCode != LedgerErrorCode.DuplicateCorrelation)
                    throw new InvalidOperationException(posted.ErrorMessage ?? "Merchant payout Ledger posting failed.");

                var concurrent = await ledger.GetByCorrelationIdAsync(request.PayoutId, cancellationToken);
                if (!concurrent.Succeeded || concurrent.Value is null)
                    throw new InvalidOperationException("Merchant payout Ledger correlation exists but cannot be loaded.");
                ValidateExisting(concurrent.Value, request, destinationAccount);
            }
        }

        receivable.ApplySettlementReceipt(
            request.PayoutId,
            request.AmountMinor,
            request.Currency,
            request.CompletedAtUtc);
        await receivables.SaveAsync(receivable, cancellationToken);

        return new MerchantPayoutFinalizationResult(
            true,
            true,
            $"ledger:{request.PayoutId:D}");
    }

    private void ValidateExisting(
        JournalEntryView journal,
        MerchantPayoutFinalizationRequest request,
        AccountId destinationAccount)
    {
        if (!string.Equals(journal.CurrencyCode, request.Currency, StringComparison.OrdinalIgnoreCase) ||
            journal.CorrelationId != request.PayoutId ||
            journal.Lines.Count != 2 ||
            !journal.Lines.Any(x =>
                x.AccountId == payoutClearingAccount.Value &&
                x.Side == LedgerSide.Debit &&
                x.AmountMinor == request.AmountMinor) ||
            !journal.Lines.Any(x =>
                x.AccountId == destinationAccount.Value &&
                x.Side == LedgerSide.Credit &&
                x.AmountMinor == request.AmountMinor))
        {
            throw new InvalidOperationException("Existing payout Ledger correlation does not match the requested finalization.");
        }
    }
}
