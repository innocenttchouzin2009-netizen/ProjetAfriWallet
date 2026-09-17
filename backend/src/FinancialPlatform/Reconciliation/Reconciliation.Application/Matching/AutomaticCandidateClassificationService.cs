using Reconciliation.Application.Queries;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Records;

namespace Reconciliation.Application.Matching;

public sealed record AutomaticCandidateClassification(
    string? InternalRecordId,
    string? ExternalRecordId,
    ReconciliationMatchType Type,
    int ConfidenceScore,
    long? AmountDifferenceMinor,
    TimeSpan? TimeDifference);

public sealed record AutomaticCandidateClassificationBatch(
    string PartnerId,
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AutomaticCandidateClassification> Candidates);

public sealed class AutomaticCandidateClassificationService
{
    private readonly TimeSpan _maximumExactTimeDifference;
    private readonly TimeSpan _maximumProbableTimeDifference;

    public AutomaticCandidateClassificationService(
        TimeSpan? maximumExactTimeDifference = null,
        TimeSpan? maximumProbableTimeDifference = null)
    {
        _maximumExactTimeDifference = maximumExactTimeDifference ?? TimeSpan.FromMinutes(10);
        _maximumProbableTimeDifference = maximumProbableTimeDifference ?? TimeSpan.FromMinutes(30);

        if (_maximumExactTimeDifference < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumExactTimeDifference));
        }

        if (_maximumProbableTimeDifference < _maximumExactTimeDifference)
        {
            throw new ArgumentException(
                "Probable time difference must be greater than or equal to exact time difference.",
                nameof(maximumProbableTimeDifference));
        }
    }

    public AutomaticCandidateClassificationBatch Classify(ReconciliationCandidateBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var remainingExternal = batch.ExternalRecords
            .OrderBy(record => record.OccurredAtUtc)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ToList();

        var candidates = new List<AutomaticCandidateClassification>();

        foreach (var internalRecord in batch.InternalRecords
                     .OrderBy(record => record.OccurredAtUtc)
                     .ThenBy(record => record.RecordId, StringComparer.Ordinal))
        {
            var ranked = remainingExternal
                .Select(externalRecord => Evaluate(internalRecord, externalRecord))
                .Where(candidate => candidate.Type is ReconciliationMatchType.Exact or ReconciliationMatchType.Partial)
                .OrderByDescending(candidate => candidate.ConfidenceScore)
                .ThenBy(candidate => Math.Abs(candidate.AmountDifferenceMinor ?? long.MaxValue))
                .ThenBy(candidate => candidate.TimeDifference ?? TimeSpan.MaxValue)
                .ThenBy(candidate => candidate.ExternalRecordId, StringComparer.Ordinal)
                .ToArray();

            if (ranked.Length == 0)
            {
                candidates.Add(new AutomaticCandidateClassification(
                    internalRecord.RecordId,
                    null,
                    ReconciliationMatchType.Unmatched,
                    0,
                    null,
                    null));
                continue;
            }

            var selected = ranked[0];
            candidates.Add(selected);
            remainingExternal.RemoveAll(record =>
                string.Equals(record.RecordId, selected.ExternalRecordId, StringComparison.Ordinal));
        }

        foreach (var externalRecord in remainingExternal)
        {
            candidates.Add(new AutomaticCandidateClassification(
                null,
                externalRecord.RecordId,
                ReconciliationMatchType.Unmatched,
                0,
                null,
                null));
        }

        return new AutomaticCandidateClassificationBatch(
            batch.PartnerId,
            batch.FromUtc,
            batch.ToUtc,
            candidates);
    }

    private AutomaticCandidateClassification Evaluate(
        InternalFinancialRecord internalRecord,
        ExternalFinancialRecord externalRecord)
    {
        var samePartner = string.Equals(
            internalRecord.PartnerId,
            externalRecord.PartnerId,
            StringComparison.Ordinal);
        var sameCurrency = string.Equals(
            internalRecord.CurrencyCode,
            externalRecord.CurrencyCode,
            StringComparison.OrdinalIgnoreCase);
        var sameReference = string.Equals(
            internalRecord.Reference,
            externalRecord.ExternalReference,
            StringComparison.OrdinalIgnoreCase);
        var sameAmount = internalRecord.AmountMinor == externalRecord.AmountMinor;
        var amountDifference = internalRecord.AmountMinor - externalRecord.AmountMinor;
        var timeDifference = internalRecord.OccurredAtUtc >= externalRecord.OccurredAtUtc
            ? internalRecord.OccurredAtUtc - externalRecord.OccurredAtUtc
            : externalRecord.OccurredAtUtc - internalRecord.OccurredAtUtc;
        var exactTime = timeDifference <= _maximumExactTimeDifference;
        var probableTime = timeDifference <= _maximumProbableTimeDifference;

        if (samePartner && sameCurrency && sameReference && sameAmount && exactTime)
        {
            return new AutomaticCandidateClassification(
                internalRecord.RecordId,
                externalRecord.RecordId,
                ReconciliationMatchType.Exact,
                100,
                amountDifference,
                timeDifference);
        }

        if (!samePartner || !sameCurrency)
        {
            return Unmatched(internalRecord.RecordId, externalRecord.RecordId, amountDifference, timeDifference);
        }

        var signals = 0;
        if (sameReference) signals += 2;
        if (sameAmount) signals += 2;
        if (probableTime) signals += 1;

        if (signals >= 3)
        {
            return new AutomaticCandidateClassification(
                internalRecord.RecordId,
                externalRecord.RecordId,
                ReconciliationMatchType.Partial,
                signals * 20,
                amountDifference,
                timeDifference);
        }

        return Unmatched(internalRecord.RecordId, externalRecord.RecordId, amountDifference, timeDifference);
    }

    private static AutomaticCandidateClassification Unmatched(
        string internalRecordId,
        string externalRecordId,
        long amountDifference,
        TimeSpan timeDifference) =>
        new(
            internalRecordId,
            externalRecordId,
            ReconciliationMatchType.Unmatched,
            0,
            amountDifference,
            timeDifference);
}
