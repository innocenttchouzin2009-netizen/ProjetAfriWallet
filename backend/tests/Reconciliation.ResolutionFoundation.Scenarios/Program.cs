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
    try { await action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var decidedAt = new DateTime(2026, 9, 18, 8, 0, 0, DateTimeKind.Utc);
var reviewId = Guid.NewGuid();
var approved = Review(reviewId, "partner-one", ReconciliationReviewStatus.Approved, decidedAt);
var reviewRepository = new FakeReviewRepository([approved]);
var resolutionRepository = new InMemoryResolutionRepository();
var service = new ReconciliationResolutionApplicationService(reviewRepository, resolutionRepository);
var scope = ReconciliationResolutionAccessScope.ForPartners("resolver-1", ["partner-one", "partner-two", "partner-three"]);
var auditScope = ReconciliationResolutionAccessScope.ForPartners("auditor-1", ["partner-one"], canReadAudit: true);
var forbiddenScope = ReconciliationResolutionAccessScope.ForPartners("resolver-x", ["partner-x"]);
var adminScope = ReconciliationResolutionAccessScope.ForAllPartners("resolver-admin");

Assert(!scope.IsAdministrator && !scope.CanReadAudit, "Ordinary resolver scope must not gain administrative or audit permissions.");
Assert(auditScope.CanReadAudit && !auditScope.IsAdministrator, "Audit permission must be separable from administrator scope.");
Assert(adminScope.IsAdministrator && adminScope.CanAccessAllPartners && adminScope.CanReadAudit,
    "Administrator scope must explicitly carry global and audit privileges.");

var command = new ResolveReconciliationReviewCommand(
    reviewId,
    ReconciliationResolutionDisposition.RecordCorrected,
    "ignored-client-resolver",
    "partner statement corrected and replayed",
    "evidence://statement/2026-09-18/42",
    decidedAt.AddMinutes(10));

var created = await service.ResolveAsync(command, scope);
Assert(created.Status == ReconciliationResolutionExecutionStatus.Created, "Final review must create a resolution.");
Assert(created.Resolution?.ResolvedBy == "resolver-1", "Resolver identity must come from access scope.");
Assert(resolutionRepository.AddCalls == 1, "Resolution must be stored once.");

var replay = await service.ResolveAsync(command with { ResolvedAtUtc = decidedAt.AddMinutes(11) }, scope);
Assert(replay.Status == ReconciliationResolutionExecutionStatus.Existing, "Equivalent replay must be idempotent.");
Assert(resolutionRepository.AddCalls == 1, "Equivalent replay must not store twice.");

var forbiddenRead = await service.GetByReviewIdAsync(reviewId, forbiddenScope);
Assert(forbiddenRead.Status == ReconciliationResolutionLookupStatus.AccessDenied, "Foreign partner resolution must be denied.");

var allowedRead = await service.GetByReviewIdAsync(reviewId, scope);
Assert(allowedRead.Status == ReconciliationResolutionLookupStatus.Found, "Authorized resolution must be readable.");

var forbiddenResolve = await service.ResolveAsync(command, forbiddenScope);
Assert(forbiddenResolve.Status == ReconciliationResolutionExecutionStatus.AccessDenied, "Foreign partner resolution must not be replayed.");

var partnerTwoId = Guid.NewGuid();
reviewRepository.Add(Review(partnerTwoId, "partner-two", ReconciliationReviewStatus.Rejected, decidedAt.AddMinutes(1)));
var partnerTwo = await service.ResolveAsync(command with
{
    ReviewId = partnerTwoId,
    Disposition = ReconciliationResolutionDisposition.VarianceAccepted,
    ResolvedAtUtc = decidedAt.AddMinutes(30)
}, scope);
Assert(partnerTwo.Status == ReconciliationResolutionExecutionStatus.Created, "Authorized second partner resolution must be created.");

var scopedList = await service.ListAsync(new ReconciliationResolutionQuery(Limit: 50), scope);
Assert(scopedList.Status == ReconciliationResolutionListStatus.Success && scopedList.Resolutions.Count == 2,
    "Scoped list must contain authorized partner resolutions.");

var onePartnerScope = ReconciliationResolutionAccessScope.ForPartners("resolver-1", ["partner-one"]);
var onePartnerList = await service.ListAsync(new ReconciliationResolutionQuery(Limit: 50), onePartnerScope);
Assert(onePartnerList.Resolutions.Count == 1 && onePartnerList.Resolutions[0].PartnerId == "partner-one",
    "Operational query must be filtered by access scope.");

var explicitForbidden = await service.ListAsync(new ReconciliationResolutionQuery(PartnerId: "partner-two"), onePartnerScope);
Assert(explicitForbidden.Status == ReconciliationResolutionListStatus.AccessDenied,
    "Explicit forbidden partner query must fail closed.");

var adminList = await service.ListAsync(
    new ReconciliationResolutionQuery(Disposition: ReconciliationResolutionDisposition.VarianceAccepted),
    adminScope);
Assert(adminList.Resolutions.Count == 1 && adminList.Resolutions[0].PartnerId == "partner-two",
    "Administrator scope must support cross-partner operational filtering.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ListAsync(new ReconciliationResolutionQuery(Limit: 501), scope),
    "Operational query limit must be bounded.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.GetByReviewIdAsync(reviewId, scope, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 final administrative control foundation scenarios: PASS");

static ReconciliationReviewItem Review(
    Guid id,
    string partnerId,
    ReconciliationReviewStatus status,
    DateTime decidedAt) =>
    new(
        id,
        partnerId,
        $"internal-{id:N}",
        $"external-{id:N}",
        ReconciliationMatchType.Partial,
        88,
        25,
        TimeSpan.FromSeconds(10),
        decidedAt.AddMinutes(-5),
        status,
        "reviewer-1",
        "review complete",
        decidedAt);

sealed class FakeReviewRepository(IEnumerable<ReconciliationReviewItem> seed) : IReconciliationReviewRepository
{
    private readonly Dictionary<Guid, ReconciliationReviewItem> items = seed.ToDictionary(x => x.ReviewId);
    public void Add(ReconciliationReviewItem item) => items[item.ReviewId] = item;
    public Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }
    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(string partnerId, ReconciliationReviewStatus? status = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<bool> TryReplaceAsync(ReconciliationReviewItem item, ReconciliationReviewStatus expectedStatus, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

sealed class InMemoryResolutionRepository : IReconciliationResolutionRepository
{
    private readonly Dictionary<Guid, ReconciliationResolution> values = new();
    public int AddCalls { get; private set; }

    public Task<ReconciliationResolution?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<ReconciliationResolution>> ListAsync(
        ReconciliationResolutionRepositoryQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<ReconciliationResolution> source = values.Values;
        if (query.PartnerIds is { Count: > 0 })
            source = source.Where(x => query.PartnerIds.Contains(x.PartnerId));
        if (query.Disposition is not null)
            source = source.Where(x => x.Disposition == query.Disposition.Value);
        if (!string.IsNullOrWhiteSpace(query.ResolvedBy))
            source = source.Where(x => x.ResolvedBy == query.ResolvedBy);
        if (query.ResolvedFromUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc >= query.ResolvedFromUtc.Value);
        if (query.ResolvedToUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc <= query.ResolvedToUtc.Value);
        return Task.FromResult<IReadOnlyList<ReconciliationResolution>>(
            source.OrderByDescending(x => x.ResolvedAtUtc).Take(query.Limit).ToArray());
    }

    public Task AddAsync(ReconciliationResolution resolution, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.TryAdd(resolution.ReviewId, resolution))
            throw new InvalidOperationException("Review already has a resolution.");
        AddCalls++;
        return Task.CompletedTask;
    }
}
