using Reconciliation.Domain.Records;

namespace Reconciliation.Application.Queries;

public sealed record AutomaticReconciliationCandidateQuery(
    string PartnerId,
    DateTime FromUtc,
    DateTime ToUtc)
{
    public static AutomaticReconciliationCandidateQuery Create(
        string partnerId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        if (string.IsNullOrWhiteSpace(partnerId))
        {
            throw new ArgumentException("Partner id is required.", nameof(partnerId));
        }

        EnsureUtc(fromUtc, nameof(fromUtc));
        EnsureUtc(toUtc, nameof(toUtc));

        if (toUtc <= fromUtc)
        {
            throw new ArgumentException("Candidate query end must be later than start.", nameof(toUtc));
        }

        return new AutomaticReconciliationCandidateQuery(partnerId.Trim(), fromUtc, toUtc);
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Candidate query timestamps must be UTC.", parameterName);
        }
    }
}

public sealed record ReconciliationCandidateBatch(
    string PartnerId,
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<InternalFinancialRecord> InternalRecords,
    IReadOnlyList<ExternalFinancialRecord> ExternalRecords);
