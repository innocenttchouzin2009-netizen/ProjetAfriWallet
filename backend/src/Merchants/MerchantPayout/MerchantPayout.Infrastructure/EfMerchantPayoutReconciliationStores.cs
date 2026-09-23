using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Payout.Infrastructure;

public sealed class EfMerchantPayoutProviderResultStore(MerchantPayoutDbContext db)
    : IMerchantPayoutProviderResultStore
{
    public async Task SaveAsync(
        MerchantPayoutProviderResultRecord result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existing = await db.ProviderResults.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResultId == result.ResultId, cancellationToken);

        if (existing is not null)
        {
            var current = Map(existing);
            if (current != result)
                throw new InvalidOperationException("Merchant payout provider result is immutable once recorded.");
            return;
        }

        db.ProviderResults.Add(new MerchantPayoutProviderResultEntity
        {
            ResultId = result.ResultId,
            PayoutId = result.PayoutId,
            MerchantId = result.MerchantId,
            Status = (int)result.Status,
            ProviderReference = result.ProviderReference,
            FailureCode = result.FailureCode,
            AmountMinor = result.AmountMinor,
            Currency = result.Currency,
            ObservedAtUtc = result.ObservedAtUtc
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantPayoutProviderResultRecord?> GetAsync(
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        if (resultId == Guid.Empty)
            throw new ArgumentException("Result id is required.", nameof(resultId));

        var row = await db.ProviderResults.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResultId == resultId, cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<MerchantPayoutProviderResultRecord>> ListForPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default)
    {
        if (payoutId == Guid.Empty)
            throw new ArgumentException("Payout id is required.", nameof(payoutId));

        var rows = await db.ProviderResults.AsNoTracking()
            .Where(x => x.PayoutId == payoutId)
            .OrderBy(x => x.ObservedAtUtc)
            .ThenBy(x => x.ResultId)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToArray();
    }

    private static MerchantPayoutProviderResultRecord Map(MerchantPayoutProviderResultEntity row) =>
        MerchantPayoutProviderResultRecord.Restore(
            row.ResultId,
            row.PayoutId,
            row.MerchantId,
            (MerchantPayoutProviderResultStatus)row.Status,
            row.ProviderReference,
            row.FailureCode,
            row.AmountMinor,
            row.Currency,
            row.ObservedAtUtc);
}

public sealed class EfMerchantPayoutReconciliationStore(MerchantPayoutDbContext db)
    : IMerchantPayoutReconciliationStore
{
    public async Task SaveAsync(
        MerchantPayoutReconciliationRecord reconciliation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reconciliation);

        var existing = await db.Reconciliations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReconciliationId == reconciliation.ReconciliationId,
                cancellationToken);

        if (existing is not null)
        {
            var current = Map(existing);
            if (current != reconciliation)
                throw new InvalidOperationException("Merchant payout reconciliation is immutable once recorded.");
            return;
        }

        db.Reconciliations.Add(new MerchantPayoutReconciliationEntity
        {
            ReconciliationId = reconciliation.ReconciliationId,
            PayoutId = reconciliation.PayoutId,
            ResultId = reconciliation.ResultId,
            MerchantId = reconciliation.MerchantId,
            Status = (int)reconciliation.Status,
            ReasonCode = reconciliation.ReasonCode,
            EvaluatedAtUtc = reconciliation.EvaluatedAtUtc
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantPayoutReconciliationRecord?> GetAsync(
        Guid reconciliationId,
        CancellationToken cancellationToken = default)
    {
        if (reconciliationId == Guid.Empty)
            throw new ArgumentException("Reconciliation id is required.", nameof(reconciliationId));

        var row = await db.Reconciliations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReconciliationId == reconciliationId,
                cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<MerchantPayoutReconciliationRecord?> GetLatestForPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default)
    {
        if (payoutId == Guid.Empty)
            throw new ArgumentException("Payout id is required.", nameof(payoutId));

        var row = await db.Reconciliations.AsNoTracking()
            .Where(x => x.PayoutId == payoutId)
            .OrderByDescending(x => x.EvaluatedAtUtc)
            .ThenByDescending(x => x.ReconciliationId)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<MerchantPayoutReconciliationRecord?> GetForResultAsync(
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        if (resultId == Guid.Empty)
            throw new ArgumentException("Result id is required.", nameof(resultId));

        var row = await db.Reconciliations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResultId == resultId, cancellationToken);

        return row is null ? null : Map(row);
    }

    private static MerchantPayoutReconciliationRecord Map(MerchantPayoutReconciliationEntity row) =>
        MerchantPayoutReconciliationRecord.Restore(
            row.ReconciliationId,
            row.PayoutId,
            row.ResultId,
            row.MerchantId,
            (MerchantPayoutReconciliationStatus)row.Status,
            row.ReasonCode,
            row.EvaluatedAtUtc);
}
