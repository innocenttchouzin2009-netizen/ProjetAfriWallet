using Reconciliation.Application.Interfaces;
using Reconciliation.Application.Queries;
using Reconciliation.Domain.Records;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var fromUtc = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
var toUtc = fromUtc.AddHours(1);
var query = AutomaticReconciliationCandidateQuery.Create(" partner-1 ", fromUtc, toUtc);
Assert(query.PartnerId == "partner-1", "Partner id must be trimmed.");

var source = new FakeDataSource(
    [
        new("i-late", "ref-2", "partner-1", "EUR", 200, fromUtc.AddMinutes(20), "ledger"),
        new("i-early", "ref-1", "partner-1", "EUR", 100, fromUtc.AddMinutes(5), "ledger"),
        new("i-wrong-partner", "ref-x", "partner-2", "EUR", 100, fromUtc.AddMinutes(10), "ledger"),
        new("i-before", "ref-y", "partner-1", "EUR", 100, fromUtc.AddTicks(-1), "ledger"),
        new("i-at-end", "ref-z", "partner-1", "EUR", 100, toUtc, "ledger")
    ],
    [
        new("e-b", "ext-2", "partner-1", "EUR", 200, fromUtc.AddMinutes(20), "partner"),
        new("e-a", "ext-1", "partner-1", "EUR", 100, fromUtc.AddMinutes(5), "partner"),
        new("e-wrong", "ext-x", "partner-9", "EUR", 100, fromUtc.AddMinutes(6), "partner")
    ]);

var service = new AutomaticReconciliationCandidateQueryService(source);
var batch = await service.QueryAsync(query);

Assert(source.InternalCalls == 1 && source.ExternalCalls == 1, "Both reconciliation sources must be queried exactly once.");
Assert(source.LastPartnerId == "partner-1", "Normalized partner id must be forwarded.");
Assert(source.LastFromUtc == fromUtc && source.LastToUtc == toUtc, "UTC window must be forwarded unchanged.");
Assert(batch.InternalRecords.Select(x => x.RecordId).SequenceEqual(["i-early", "i-late"]), "Internal candidates must be eligible and deterministic.");
Assert(batch.ExternalRecords.Select(x => x.RecordId).SequenceEqual(["e-a", "e-b"]), "External candidates must be eligible and deterministic.");
Assert(batch.InternalRecords.All(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc), "Internal candidate window must be half-open.");
Assert(batch.ExternalRecords.All(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc), "External candidate window must be half-open.");

var empty = await new AutomaticReconciliationCandidateQueryService(new FakeDataSource([], []))
    .QueryAsync(AutomaticReconciliationCandidateQuery.Create("partner-empty", fromUtc, toUtc));
Assert(empty.InternalRecords.Count == 0 && empty.ExternalRecords.Count == 0, "Empty sources must return an empty candidate batch.");

AssertThrows<ArgumentException>(
    () => AutomaticReconciliationCandidateQuery.Create(" ", fromUtc, toUtc),
    "Blank partner id must be rejected.");
AssertThrows<ArgumentException>(
    () => AutomaticReconciliationCandidateQuery.Create("p", DateTime.SpecifyKind(fromUtc, DateTimeKind.Local), toUtc),
    "Non-UTC start must be rejected.");
AssertThrows<ArgumentException>(
    () => AutomaticReconciliationCandidateQuery.Create("p", fromUtc, fromUtc),
    "Non-positive query window must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.QueryAsync(query, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-3 automatic candidate query scenarios: PASS");

sealed class FakeDataSource(
    IReadOnlyCollection<InternalFinancialRecord> internalRecords,
    IReadOnlyCollection<ExternalFinancialRecord> externalRecords) : IReconciliationDataSource
{
    public int InternalCalls { get; private set; }
    public int ExternalCalls { get; private set; }
    public string? LastPartnerId { get; private set; }
    public DateTime LastFromUtc { get; private set; }
    public DateTime LastToUtc { get; private set; }

    public Task<IReadOnlyCollection<InternalFinancialRecord>> GetInternalRecordsAsync(
        string partnerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InternalCalls++;
        Capture(partnerId, fromUtc, toUtc);
        return Task.FromResult(internalRecords);
    }

    public Task<IReadOnlyCollection<ExternalFinancialRecord>> GetExternalRecordsAsync(
        string partnerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExternalCalls++;
        Capture(partnerId, fromUtc, toUtc);
        return Task.FromResult(externalRecords);
    }

    private void Capture(string partnerId, DateTime fromUtc, DateTime toUtc)
    {
        LastPartnerId = partnerId;
        LastFromUtc = fromUtc;
        LastToUtc = toUtc;
    }
}
