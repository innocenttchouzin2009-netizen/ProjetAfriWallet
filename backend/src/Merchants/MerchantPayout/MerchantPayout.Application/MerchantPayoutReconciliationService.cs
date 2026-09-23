using AfriWallet.Merchants.Payout.Domain;

namespace AfriWallet.Merchants.Payout.Application;

public sealed record ReconcileMerchantPayoutProviderResultCommand(
    Guid ResultId,
    Guid PayoutId,
    MerchantPayoutProviderResultStatus Status,
    string? ProviderReference,
    string? FailureCode,
    long AmountMinor,
    string Currency,
    DateTimeOffset ObservedAtUtc);

public sealed record MerchantPayoutReconciliationResult(
    Guid ReconciliationId,
    Guid PayoutId,
    Guid ResultId,
    string MerchantId,
    MerchantPayoutReconciliationStatus Status,
    string ReasonCode,
    DateTimeOffset EvaluatedAtUtc);

public sealed class MerchantPayoutReconciliationService(
    IMerchantPayoutRepository payouts,
    IMerchantPayoutProviderResultStore providerResults,
    IMerchantPayoutReconciliationStore reconciliations,
    MerchantPayoutReconciliationPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<MerchantPayoutReconciliationResult> RecordAndReconcileAsync(
        ReconcileMerchantPayoutProviderResultCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ResultId == Guid.Empty)
            throw new ArgumentException("Provider result id is required.", nameof(command));
        if (command.PayoutId == Guid.Empty)
            throw new ArgumentException("Payout id is required.", nameof(command));

        var existingReconciliation = await reconciliations.GetForResultAsync(
            command.ResultId,
            cancellationToken);

        if (existingReconciliation is not null)
            return Map(existingReconciliation);

        var payout = await payouts.GetAsync(command.PayoutId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout not found.");

        var providerResult = MerchantPayoutProviderResultRecord.Restore(
            command.ResultId,
            payout.PayoutId,
            payout.MerchantId,
            command.Status,
            command.ProviderReference,
            command.FailureCode,
            command.AmountMinor,
            command.Currency,
            command.ObservedAtUtc);

        await providerResults.SaveAsync(providerResult, cancellationToken);

        var evaluatedAtUtc = timeProvider.GetUtcNow();
        if (evaluatedAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidOperationException("Reconciliation clock must return UTC timestamps.");
        if (evaluatedAtUtc < providerResult.ObservedAtUtc)
            throw new InvalidOperationException("Reconciliation cannot precede the provider result observation.");

        var reconciliation = policy.Evaluate(
            payout,
            providerResult,
            evaluatedAtUtc);

        await reconciliations.SaveAsync(reconciliation, cancellationToken);
        return Map(reconciliation);
    }

    public async Task<MerchantPayoutReconciliationResult?> GetLatestAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default)
    {
        if (payoutId == Guid.Empty)
            throw new ArgumentException("Payout id is required.", nameof(payoutId));

        var reconciliation = await reconciliations.GetLatestForPayoutAsync(
            payoutId,
            cancellationToken);

        return reconciliation is null ? null : Map(reconciliation);
    }

    private static MerchantPayoutReconciliationResult Map(
        MerchantPayoutReconciliationRecord reconciliation) =>
        new(
            reconciliation.ReconciliationId,
            reconciliation.PayoutId,
            reconciliation.ResultId,
            reconciliation.MerchantId,
            reconciliation.Status,
            reconciliation.ReasonCode,
            reconciliation.EvaluatedAtUtc);
}
