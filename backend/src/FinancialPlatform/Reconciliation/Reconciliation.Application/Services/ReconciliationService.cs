using Reconciliation.Application.Interfaces;
using Reconciliation.Application.Matching;
using Reconciliation.Domain.Exceptions;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Runs;

namespace Reconciliation.Application.Services;

public sealed class ReconciliationService
{
    private readonly IReconciliationDataSource _dataSource;
    private readonly IReconciliationRepository _repository;
    private readonly ReconciliationMatcher _matcher;

    public ReconciliationService(
        IReconciliationDataSource dataSource,
        IReconciliationRepository repository,
        ReconciliationMatcher matcher)
    {
        _dataSource = dataSource;
        _repository = repository;
        _matcher = matcher;
    }

    public async Task<ReconciliationRun> RunAsync(
        string partnerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var run = new ReconciliationRun(
            Guid.NewGuid(),
            partnerId,
            fromUtc,
            toUtc);

        await _repository.AddRunAsync(
            run,
            cancellationToken);

        run.Start();

        var internalRecords =
            await _dataSource.GetInternalRecordsAsync(
                partnerId,
                fromUtc,
                toUtc,
                cancellationToken);

        var externalRecords =
            await _dataSource.GetExternalRecordsAsync(
                partnerId,
                fromUtc,
                toUtc,
                cancellationToken);

        var remainingExternal =
            externalRecords.ToList();

        foreach (var internalRecord in internalRecords)
        {
            var candidate =
                remainingExternal.FirstOrDefault(x =>
                    string.Equals(
                        x.ExternalReference,
                        internalRecord.Reference,
                        StringComparison.OrdinalIgnoreCase));

            if (candidate is null)
            {
                run.AddException(
                    new ReconciliationException(
                        Guid.NewGuid(),
                        "EXTERNAL_RECORD_MISSING",
                        "No external record was found.",
                        internalRecord.RecordId,
                        null));

                continue;
            }

            var match =
                _matcher.Match(
                    internalRecord,
                    candidate);

            run.AddMatch(match);

            remainingExternal.Remove(candidate);

            if (match.Type ==
                ReconciliationMatchType.Partial)
            {
                run.AddException(
                    new ReconciliationException(
                        Guid.NewGuid(),
                        "RECONCILIATION_DIFFERENCE",
                        "Internal and external records differ.",
                        internalRecord.RecordId,
                        candidate.RecordId));
            }
        }

        foreach (var externalRecord in remainingExternal)
        {
            run.AddException(
                new ReconciliationException(
                    Guid.NewGuid(),
                    "INTERNAL_RECORD_MISSING",
                    "External record has no internal counterpart.",
                    null,
                    externalRecord.RecordId));
        }

        run.Complete();

        return run;
    }
}
