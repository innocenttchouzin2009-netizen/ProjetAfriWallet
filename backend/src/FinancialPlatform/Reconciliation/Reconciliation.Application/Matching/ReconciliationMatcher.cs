using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Records;

namespace Reconciliation.Application.Matching;

public sealed class ReconciliationMatcher
{
    private readonly TimeSpan _maximumTimeDifference;

    public ReconciliationMatcher(
        TimeSpan? maximumTimeDifference = null)
    {
        _maximumTimeDifference =
            maximumTimeDifference ?? TimeSpan.FromMinutes(10);
    }

    public ReconciliationMatch Match(
        InternalFinancialRecord internalRecord,
        ExternalFinancialRecord externalRecord)
    {
        var amountDifference =
            internalRecord.AmountMinor -
            externalRecord.AmountMinor;

        var timeDifference =
            internalRecord.OccurredAtUtc >
            externalRecord.OccurredAtUtc
                ? internalRecord.OccurredAtUtc -
                  externalRecord.OccurredAtUtc
                : externalRecord.OccurredAtUtc -
                  internalRecord.OccurredAtUtc;

        var samePartner =
            string.Equals(
                internalRecord.PartnerId,
                externalRecord.PartnerId,
                StringComparison.OrdinalIgnoreCase);

        var sameCurrency =
            string.Equals(
                internalRecord.CurrencyCode,
                externalRecord.CurrencyCode,
                StringComparison.OrdinalIgnoreCase);

        var sameReference =
            string.Equals(
                internalRecord.Reference,
                externalRecord.ExternalReference,
                StringComparison.OrdinalIgnoreCase);

        var type =
            samePartner &&
            sameCurrency &&
            sameReference &&
            amountDifference == 0 &&
            timeDifference <= _maximumTimeDifference
                ? ReconciliationMatchType.Exact
                : samePartner &&
                  sameCurrency &&
                  sameReference
                    ? ReconciliationMatchType.Partial
                    : ReconciliationMatchType.Unmatched;

        return new ReconciliationMatch(
            Guid.NewGuid(),
            internalRecord.RecordId,
            externalRecord.RecordId,
            type,
            amountDifference,
            timeDifference,
            DateTime.UtcNow);
    }
}
