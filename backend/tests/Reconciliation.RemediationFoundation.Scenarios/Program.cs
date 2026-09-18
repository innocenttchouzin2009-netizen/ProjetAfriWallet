using Reconciliation.Application.Remediation;
using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Remediation;
using Reconciliation.Domain.Resolutions;

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

var reviewId = Guid.NewGuid();
var resolvedAt = new DateTime(2026, 9, 18, 9, 0, 0, DateTimeKind.Utc);
var resolution = ReconciliationResolution.Create(
    reviewId,
    "partner-a",
    "internal-1",
    "external-1",
    ReconciliationResolutionDisposition.RecordCorrected,
    "Mismatch investigated and correction approved.",
    "evidence://resolution/1",
    "resolver-1",
    resolvedAt);

var resolutionRepository = new InMemoryResolutionRepository(resolution);
var actionRepository = new InMemoryCorrectiveActionRepository();
var service = new ReconciliationCorrectiveActionApplicationService(
    resolutionRepository,
    actionRepository);
var access = ReconciliationResolutionAccessScope.ForPartners("operator-1", new[] { "partner-a" });

var created = await service.CreateAsync(
    new CreateReconciliationCorrectiveActionCommand(
        reviewId,
        " source_record.fix ",
        "Correct the source record and retain the operational proof.",
        resolvedAt.AddMinutes(5)),
    access);

Assert(created.Status == ReconciliationCorrectiveActionExecutionStatus.Created, "Corrective action must be created.");
Assert(created.Action is not null, "Created corrective action is required.");
Assert(created.Action!.ResolutionId == resolution.ResolutionId, "Resolution link must be preserved.");
Assert(created.Action.ReviewId == reviewId, "Review link must be preserved.");
Assert(created.Action.ActionCode == "SOURCE_RECORD.FIX", "Action code must be canonicalized.");
Assert(created.Action.CreatedBy == "operator-1", "Actor must come from access scope.");
Assert(created.Action.Status == ReconciliationCorrectiveActionStatus.Pending, "New action must be pending.");
Assert(actionRepository.AddCalls == 1, "Action must be added once.");

var replay = await service.CreateAsync(
    new CreateReconciliationCorrectiveActionCommand(
        reviewId,
        "SOURCE_RECORD.FIX",
        "Correct the source record and retain the operational proof.",
        resolvedAt.AddMinutes(10)),
    access);
Assert(replay.Status == ReconciliationCorrectiveActionExecutionStatus.Existing, "Equivalent retry must be idempotent.");
Assert(actionRepository.AddCalls == 1, "Equivalent retry must not add twice.");

try
{
    await service.CreateAsync(
        new CreateReconciliationCorrectiveActionCommand(
            reviewId,
            "SOURCE_RECORD.REPLACE",
            "Different action.",
            resolvedAt.AddMinutes(10)),
        access);
    throw new InvalidOperationException("Expected conflicting corrective action.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("different reconciliation corrective action", StringComparison.Ordinal))
{
}

var lookup = await service.GetByResolutionIdAsync(resolution.ResolutionId, access);
Assert(lookup.Status == ReconciliationCorrectiveActionLookupStatus.Found, "Authorized lookup must succeed.");

var deniedAccess = ReconciliationResolutionAccessScope.ForPartners("operator-2", new[] { "partner-b" });
var denied = await service.GetByResolutionIdAsync(resolution.ResolutionId, deniedAccess);
Assert(denied.Status == ReconciliationCorrectiveActionLookupStatus.AccessDenied, "Foreign partner lookup must be denied.");

var deniedCreate = await new ReconciliationCorrectiveActionApplicationService(
        resolutionRepository,
        new InMemoryCorrectiveActionRepository())
    .CreateAsync(
        new CreateReconciliationCorrectiveActionCommand(
            reviewId,
            "SOURCE_RECORD.FIX",
            "Correct source.",
            resolvedAt.AddMinutes(1)),
        deniedAccess);
Assert(deniedCreate.Status == ReconciliationCorrectiveActionExecutionStatus.AccessDenied, "Foreign partner create must be denied.");

var notEligibleResolution = ReconciliationResolution.Create(
    Guid.NewGuid(),
    "partner-a",
    "internal-2",
    "external-2",
    ReconciliationResolutionDisposition.MatchConfirmed,
    "Match confirmed.",
    "evidence://resolution/2",
    "resolver-1",
    resolvedAt);
var notEligible = await new ReconciliationCorrectiveActionApplicationService(
        new InMemoryResolutionRepository(notEligibleResolution),
        new InMemoryCorrectiveActionRepository())
    .CreateAsync(
        new CreateReconciliationCorrectiveActionCommand(
            notEligibleResolution.ReviewId,
            "SOURCE_RECORD.FIX",
            "Should not be created.",
            resolvedAt.AddMinutes(1)),
        access);
Assert(notEligible.Status == ReconciliationCorrectiveActionExecutionStatus.ResolutionNotEligible, "Only RecordCorrected resolutions are eligible.");

var missing = await new ReconciliationCorrectiveActionApplicationService(
        new InMemoryResolutionRepository(),
        new InMemoryCorrectiveActionRepository())
    .CreateAsync(
        new CreateReconciliationCorrectiveActionCommand(
            Guid.NewGuid(),
            "SOURCE_RECORD.FIX",
            "Missing resolution.",
            resolvedAt.AddMinutes(1)),
        access);
Assert(missing.Status == ReconciliationCorrectiveActionExecutionStatus.ResolutionNotFound, "Missing resolution must be reported.");

var direct = ReconciliationCorrectiveAction.Create(
    resolution.ResolutionId,
    reviewId,
    "partner-a",
    "internal-1",
    "external-1",
    "SOURCE_RECORD.FIX",
    "Direct lifecycle scenario.",
    "operator-1",
    resolvedAt.AddMinutes(1));
direct.Complete("operator-2", "evidence://action/complete", resolvedAt.AddMinutes(2));
Assert(direct.Status == ReconciliationCorrectiveActionStatus.Completed, "Pending action must complete.");
Assert(direct.CompletionEvidenceReference == "evidence://action/complete", "Completion evidence must be retained.");
AssertThrows<InvalidOperationException>(
    () => direct.Cancel("operator-3", "Too late.", resolvedAt.AddMinutes(3)),
    "Completed action must be terminal.");

var cancellable = ReconciliationCorrectiveAction.Create(
    resolution.ResolutionId,
    reviewId,
    "partner-a",
    "internal-1",
    "external-1",
    "SOURCE_RECORD.FIX",
    "Cancellation lifecycle scenario.",
    "operator-1",
    resolvedAt.AddMinutes(1));
cancellable.Cancel("operator-2", "Correction is no longer required.", resolvedAt.AddMinutes(2));
Assert(cancellable.Status == ReconciliationCorrectiveActionStatus.Cancelled, "Pending action must cancel.");
AssertThrows<InvalidOperationException>(
    () => cancellable.Complete("operator-3", "evidence://late", resolvedAt.AddMinutes(3)),
    "Cancelled action must be terminal.");

AssertThrows<ArgumentException>(
    () => ReconciliationCorrectiveAction.Create(
        resolution.ResolutionId,
        reviewId,
        "partner-a",
        "internal-1",
        "external-1",
        "bad action code!",
        "Invalid code.",
        "operator-1",
        resolvedAt),
    "Unsupported action code characters must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.GetByResolutionIdAsync(resolution.ResolutionId, access, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}

Console.WriteLine("AFW-BE-RECONCILIATION-REMEDIATION-1 corrective action foundation scenarios: PASS");

sealed class InMemoryCorrectiveActionRepository : IReconciliationCorrectiveActionRepository
{
    private readonly Dictionary<Guid, ReconciliationCorrectiveAction> byResolutionId = new();
    public int AddCalls { get; private set; }

    public Task<ReconciliationCorrectiveAction?> GetByResolutionIdAsync(
        Guid resolutionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byResolutionId.TryGetValue(resolutionId, out var action);
        return Task.FromResult(action);
    }

    public Task AddAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byResolutionId[action.ResolutionId] = action;
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byResolutionId[action.ResolutionId] = action;
        return Task.CompletedTask;
    }
}

sealed class InMemoryResolutionRepository(params ReconciliationResolution[] values) : IReconciliationResolutionRepository
{
    private readonly Dictionary<Guid, ReconciliationResolution> byReviewId =
        values.ToDictionary(value => value.ReviewId);

    public Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byReviewId.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<ReconciliationResolution>> ListAsync(
        ReconciliationResolutionRepositoryQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ReconciliationResolution>>(byReviewId.Values.ToArray());
    }

    public Task AddAsync(
        ReconciliationResolution resolution,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byReviewId[resolution.ReviewId] = resolution;
        return Task.CompletedTask;
    }
}
