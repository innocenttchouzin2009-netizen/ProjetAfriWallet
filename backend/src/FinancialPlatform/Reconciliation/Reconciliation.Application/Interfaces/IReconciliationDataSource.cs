using Reconciliation.Domain.Records;

namespace Reconciliation.Application.Interfaces;

public interface IReconciliationDataSource
{
    Task<IReadOnlyCollection<InternalFinancialRecord>>
        GetInternalRecordsAsync(
            string partnerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExternalFinancialRecord>>
        GetExternalRecordsAsync(
            string partnerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken);
}
