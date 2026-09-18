using Reconciliation.Application.Resolution;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Resolutions;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var decidedAt = new DateTime(2026, 9, 18, 8, 0, 0, DateTimeKind.Utc);
var reviewId = Guid.NewGuid();
var approved = new ReconciliationReviewItem(
    reviewId,
    "partner-one",
    "internal-1",
    "external-1",
    ReconciliationMatchType.Partial,
    88,
    25,
    TimeSpan.FromSeconds(10),
    decidedAt.AddMinutes(-5),
    ReconciliationReviewStatus.Approved,
    "reviewer-1",
    "partner statement verified",
    decidedAt);

var reviewRepository = new FakeReviewRepository([approved]);
var resolutionRepository = new InMemoryResolutionRepository();
var service = new ReconciliationResolutionApplicationService(reviewRepository, resolutionRepository);

var command = new ResolveReconciliationReviewCommand(
    reviewId,
    ReconciliationResolutionDisposition.RecordCorrected,
    "resolver-1",
    "partner statement corrected and replayed",
    "evidence://statement/2026-09-18/42",
    decidedAt.AddMinutes(10));

var created = await service.ResolveAsync(command);
Assert(created.Status == ReconciliationResolutionExecutionStatus.Created, "Final review must create a resolution.");
Assert(created.Resolution is not null, "Created resolution is required.");
Assert(created.Resolution!.ReviewId == reviewId, "Resolution must retain review id.");
Assert(created.Resolution.PartnerId == "partner-one", "Resolution must retain partner id.");
Assert(created.Resolution.InternalRecordId == "internal-1", "Internal record id must be retained.");
Assert(created.Resolution.ExternalRecordId == "external-1", "External record id must be retained.");
Assert(created.Resolution.ResolutionId != Guid.Empty, "Resolution id must be generated.");
Assert(resolutionRepository.AddCalls == 1, "Resolution must be stored once.");

var replay = await service.ResolveAsync(command);
Assert(replay.Status == ReconciliationResolutionExecutionStatus.Existing, "Equivalent replay must be idempotent.");
Assert(resolutionRepository.AddCalls == 1, "Equivalent replay must not store twice.");

await AssertThrowsAsync<InvalidOperationException>(
    () => service.ResolveAsync(command with { Rationale = "different outcome evidence" }),
    "Conflicting replay must fail closed.");

var pendingId = Guid.NewGuid();
reviewRepository.Add(new ReconciliationReviewItem(
    pendingId,
    "partner-one",
    "internal-2",
    null,
    ReconciliationMatchType.Unmatched,
    30,
    null,
    null,
    decidedAt,
    ReconciliationReviewStatus.PendingReview,
    null,
    null,
    null));
var pending = await service.ResolveAsync(command with { ReviewId = pendingId, ResolvedAtUtc = decidedAt.AddMinutes(20) });
Assert(pending.Status == ReconciliationResolutionExecutionStatus.ReviewNotFinal, "Pending review cannot be resolved.");

var escalatedId = Guid.NewGuid();
reviewRepository.Add(new ReconciliationReviewItem(
    escalatedId,
    "partner-one",
    null,
    "external-3",
    ReconciliationMatchType.Unmatched,
    20,
    null,
    null,
    decidedAt,
    ReconciliationReviewStatus.Escalated,
    "reviewer-2",
    "needs senior review",
    decidedAt.AddMinutes(1)));
var escalated = await service.ResolveAsync(command with { ReviewId = escalatedId, ResolvedAtUtc = decidedAt.AddMinutes(20) });
Assert(escalated.Status == ReconciliationResolutionExecutionStatus.ReviewNotFinal, "Escalated review cannot be resolved.");

var missing = await service.ResolveAsync(command with { ReviewId = Guid.NewGuid() });
Assert(missing.Status == ReconciliationResolutionExecutionStatus.ReviewNotFound, "Unknown review must return ReviewNotFound.");

var rejectedId = Guid.NewGuid();
reviewRepository.Add(new ReconciliationReviewItem(
    rejectedId,
    "partner-two",
    null,
    "external-4",
    ReconciliationMatchType.Unmatched,
    15,
    null,
    null,
    decidedAt,
    ReconciliationReviewStatus.Rejected,
    "reviewer-3",
    "candidate is not a valid match",
    decidedAt.AddMinutes(2)));
var rejected = await service.ResolveAsync(command with
{
    ReviewId = rejectedId,
    Disposition = ReconciliationResolutionDisposition.VarianceAccepted,
    ResolvedAtUtc = decidedAt.AddMinutes(30)
});
Assert(rejected.Status == ReconciliationResolutionExecutionStatus.Created, "Rejected terminal review may be resolved.");
Assert(rejected.Resolution?.ExternalRecordId == "external-4", "Single external record reference must be preserved.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ResolveAsync(command with { EvidenceReference = " " }),
    "Resolution requires corrective evidence.");
await AssertThrowsAsync<ArgumentException>(
    () => service.ResolveAsync(command with { ResolvedAtUtc = decidedAt.AddMinutes(-1) }),
    "Resolution cannot predate review decision.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.GetByReviewIdAsync(reviewId, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 domain and application foundation scenarios: PASS");

sealed class FakeReviewRepository(IEnumerable<ReconciliationReviewItem> seed) : IReconciliationReviewRepository
{
    private readonly Dictionary<Guid, ReconciliationReviewItem> items = seed.ToDictionary(x => x.ReviewId);

    public void Add(ReconciliationReviewItem item) => items[item.ReviewId] = item;

    public Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> TryReplaceAsync(
        ReconciliationReviewItem item,
        ReconciliationReviewStatus expectedStatus,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

sealed class InMemoryResolutionRepository : IReconciliationResolutionRepository
{
    private readonly Dictionary<Guid, ReconciliationResolution> byReviewId = new();
    public int AddCalls { get; private set; }

    public Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byReviewId.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }

    public Task AddAsync(
        ReconciliationResolution resolution,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!byReviewId.TryAdd(resolution.ReviewId, resolution))
            throw new InvalidOperationException("Review already has a resolution.");
        AddCalls++;
        return Task.CompletedTask;
    }
}
