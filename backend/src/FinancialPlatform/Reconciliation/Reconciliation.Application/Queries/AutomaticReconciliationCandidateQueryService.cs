using Reconciliation.Application.Interfaces;
using Reconciliation.Domain.Records;

namespace Reconciliation.Application.Queries;

public sealed class AutomaticReconciliationCandidateQueryService(
    IReconciliationDataSource dataSource)
{
    public async Task<ReconciliationCandidateBatch> QueryAsync(
        AutomaticReconciliationCandidateQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = AutomaticReconciliationCandidateQuery.Create(
            query.PartnerId,
            query.FromUtc,
            query.ToUtc);

        var internalTask = dataSource.GetInternalRecordsAsync(
            normalized.PartnerId,
            normalized.FromUtc,
            normalized.ToUtc,
            cancellationToken);
        var externalTask = dataSource.GetExternalRecordsAsync(
            normalized.PartnerId,
            normalized.FromUtc,
            normalized.ToUtc,
            cancellationToken);

        await Task.WhenAll(internalTask, externalTask);

        var internalRecords = (await internalTask)
            .Where(record => IsEligible(record.PartnerId, record.OccurredAtUtc, normalized))
            .OrderBy(record => record.OccurredAtUtc)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ToArray();

        var externalRecords = (await externalTask)
            .Where(record => IsEligible(record.PartnerId, record.OccurredAtUtc, normalized))
            .OrderBy(record => record.OccurredAtUtc)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ToArray();

        return new ReconciliationCandidateBatch(
            normalized.PartnerId,
            normalized.FromUtc,
            normalized.ToUtc,
            internalRecords,
            externalRecords);
    }

    private static bool IsEligible(
        string partnerId,
        DateTime occurredAtUtc,
        AutomaticReconciliationCandidateQuery query) =>
        string.Equals(partnerId, query.PartnerId, StringComparison.Ordinal) &&
        occurredAtUtc >= query.FromUtc &&
        occurredAtUtc < query.ToUtc;
}
