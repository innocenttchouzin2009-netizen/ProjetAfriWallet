using Reconciliation.Application.Interfaces;
using Reconciliation.Domain.Records;

namespace Reconciliation.Infrastructure.DataSources;

public sealed class SandboxReconciliationDataSource :
    IReconciliationDataSource
{
    private readonly List<InternalFinancialRecord> _internal = [];
    private readonly List<ExternalFinancialRecord> _external = [];

    public void AddInternal(
        InternalFinancialRecord record) =>
        _internal.Add(record);

    public void AddExternal(
        ExternalFinancialRecord record) =>
        _external.Add(record);

    public Task<IReadOnlyCollection<InternalFinancialRecord>>
        GetInternalRecordsAsync(
            string partnerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken)
    {
        var records = _internal
            .Where(x =>
                x.PartnerId == partnerId &&
                x.OccurredAtUtc >= fromUtc &&
                x.OccurredAtUtc <= toUtc)
            .ToArray();

        return Task.FromResult<
            IReadOnlyCollection<InternalFinancialRecord>>(records);
    }

    public Task<IReadOnlyCollection<ExternalFinancialRecord>>
        GetExternalRecordsAsync(
            string partnerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken)
    {
        var records = _external
            .Where(x =>
                x.PartnerId == partnerId &&
                x.OccurredAtUtc >= fromUtc &&
                x.OccurredAtUtc <= toUtc)
            .ToArray();

        return Task.FromResult<
            IReadOnlyCollection<ExternalFinancialRecord>>(records);
    }
}
