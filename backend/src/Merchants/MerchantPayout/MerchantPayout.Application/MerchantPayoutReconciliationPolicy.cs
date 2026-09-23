using AfriWallet.Merchants.Payout.Domain;

namespace AfriWallet.Merchants.Payout.Application;

public sealed class MerchantPayoutReconciliationPolicy
{
    public MerchantPayoutReconciliationRecord Evaluate(
        MerchantPayoutExecution payout,
        MerchantPayoutProviderResultRecord result,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(payout);
        ArgumentNullException.ThrowIfNull(result);

        if (evaluatedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Evaluation timestamp must be UTC.", nameof(evaluatedAtUtc));

        if (payout.PayoutId != result.PayoutId)
            throw new InvalidOperationException("Provider result belongs to a different payout.");

        if (!string.Equals(payout.MerchantId, result.MerchantId, StringComparison.Ordinal))
            throw new InvalidOperationException("Provider result belongs to a different merchant.");

        if (payout.Status is not (MerchantPayoutStatus.Succeeded or MerchantPayoutStatus.Failed))
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.ManualReview,
                MerchantPayoutReconciliationReasonCode.PayoutNotTerminal,
                evaluatedAtUtc);
        }

        var expectedProviderStatus = payout.Status == MerchantPayoutStatus.Succeeded
            ? MerchantPayoutProviderResultStatus.Succeeded
            : MerchantPayoutProviderResultStatus.Failed;

        if (result.Status != expectedProviderStatus)
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.Mismatch,
                MerchantPayoutReconciliationReasonCode.ProviderStatusMismatch,
                evaluatedAtUtc);
        }

        if (result.AmountMinor != payout.AmountMinor)
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.Mismatch,
                MerchantPayoutReconciliationReasonCode.AmountMismatch,
                evaluatedAtUtc);
        }

        if (!string.Equals(result.Currency, payout.Currency, StringComparison.Ordinal))
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.Mismatch,
                MerchantPayoutReconciliationReasonCode.CurrencyMismatch,
                evaluatedAtUtc);
        }

        if (payout.Status == MerchantPayoutStatus.Succeeded &&
            !string.Equals(result.ProviderReference, payout.ProviderReference, StringComparison.Ordinal))
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.Mismatch,
                MerchantPayoutReconciliationReasonCode.ProviderReferenceMismatch,
                evaluatedAtUtc);
        }

        if (payout.Status == MerchantPayoutStatus.Failed &&
            !string.Equals(result.FailureCode, payout.FailureCode, StringComparison.Ordinal))
        {
            return Create(
                payout,
                result,
                MerchantPayoutReconciliationStatus.Mismatch,
                MerchantPayoutReconciliationReasonCode.FailureCodeMismatch,
                evaluatedAtUtc);
        }

        return Create(
            payout,
            result,
            MerchantPayoutReconciliationStatus.Matched,
            MerchantPayoutReconciliationReasonCode.Matched,
            evaluatedAtUtc);
    }

    private static MerchantPayoutReconciliationRecord Create(
        MerchantPayoutExecution payout,
        MerchantPayoutProviderResultRecord result,
        MerchantPayoutReconciliationStatus status,
        string reasonCode,
        DateTimeOffset evaluatedAtUtc) =>
        MerchantPayoutReconciliationRecord.Create(
            payout.PayoutId,
            result.ResultId,
            payout.MerchantId,
            status,
            reasonCode,
            evaluatedAtUtc);
}
