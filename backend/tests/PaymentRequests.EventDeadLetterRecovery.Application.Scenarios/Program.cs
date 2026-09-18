using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 18, 19, 30, 0, TimeSpan.Zero);
var requestId = PaymentRequestId.From(Guid.NewGuid());
var eventId = Guid.NewGuid();

PaymentRequestEventOutboxItem DeadLetter(Guid id, int attemptCount = 3) =>
    new(
        new PaymentRequestEventEnvelope(
            id,
            requestId,
            "payment_request.paid",
            now.AddMinutes(-10),
            "{}"),
        PaymentRequestEventOutboxStatus.DeadLetter,
        attemptCount,
        now.AddMinutes(-10),
        now.AddMinutes(-1),
        null,
        now.AddMinutes(-1),
        null,
        "provider failure");

PaymentRequestEventAttempt[] Attempts(Guid id, int count = 3) =>
    Enumerable.Range(1, count)
        .Select(n => new PaymentRequestEventAttempt(
            Guid.NewGuid(),
            id,
            n,
            now.AddMinutes(-10 + n),
            now.AddMinutes(-10 + n).AddSeconds(1),
            n == count
                ? PaymentRequestEventAttemptOutcome.DeadLetter
                : PaymentRequestEventAttemptOutcome.RetryScheduled,
            "failure",
            n == count ? null : now.AddMinutes(-9 + n)))
        .ToArray();

PaymentRequestEventDeadLetterReplayRequest Request(Guid id, int priorReplayCount = 0) =>
    new(
        id,
        "ops.user",
        "provider configuration corrected",
        now,
        now.AddMinutes(2),
        priorReplayCount);

var approvedStore = new RecordingRecoveryStore(DeadLetter(eventId), tryRequeueResult: true);
var approvedLedger = new RecordingAttemptLedger(Attempts(eventId));
var approvedService = new PaymentRequestEventDeadLetterRecoveryService(
    approvedStore,
    approvedLedger,
    new PaymentRequestEventDeadLetterReplayPolicy(new(3)));

var approved = await approvedService.RecoverAsync(Request(eventId));
Assert(approved.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.Requeued, "Approved replay must requeue.");
Assert(approved.Requeued, "Approved replay must report Requeued.");
Assert(approvedStore.GetCalls == 1, "Recovery must load the outbox item exactly once.");
Assert(approvedLedger.ListCalls == 1, "Recovery must load attempt history exactly once.");
Assert(approvedStore.TryRequeueCalls == 1, "Approved replay must invoke TryRequeueAsync exactly once.");
Assert(approvedStore.LastPlan is not null, "Approved replay must forward the policy plan.");
Assert(approvedStore.LastPlan!.ExpectedAttemptCount == 3, "Replay plan must preserve durable attempt count.");
Assert(approvedStore.LastPlan.ReplayOrdinal == 1, "Replay plan must preserve policy replay ordinal.");

var missingStore = new RecordingRecoveryStore(null, tryRequeueResult: true);
var missingLedger = new RecordingAttemptLedger(Attempts(eventId));
var missingService = new PaymentRequestEventDeadLetterRecoveryService(
    missingStore,
    missingLedger,
    new PaymentRequestEventDeadLetterReplayPolicy());

var missing = await missingService.RecoverAsync(Request(eventId));
Assert(missing.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.EventNotFound, "Unknown event must return EventNotFound.");
Assert(missingLedger.ListCalls == 0, "Unknown event must not load Attempt Ledger.");
Assert(missingStore.TryRequeueCalls == 0, "Unknown event must never call TryRequeueAsync.");

var retryItem = DeadLetter(eventId) with { Status = PaymentRequestEventOutboxStatus.Retry };
var notDeadStore = new RecordingRecoveryStore(retryItem, tryRequeueResult: true);
var notDeadLedger = new RecordingAttemptLedger(Attempts(eventId));
var notDeadService = new PaymentRequestEventDeadLetterRecoveryService(
    notDeadStore,
    notDeadLedger,
    new PaymentRequestEventDeadLetterReplayPolicy());

var notDead = await notDeadService.RecoverAsync(Request(eventId));
Assert(notDead.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.NotDeadLetter, "Non-dead-letter item must be rejected.");
Assert(notDeadStore.TryRequeueCalls == 0, "Policy rejection must never call TryRequeueAsync.");

var mismatchStore = new RecordingRecoveryStore(DeadLetter(eventId, 3), tryRequeueResult: true);
var mismatchLedger = new RecordingAttemptLedger(Attempts(eventId, 2));
var mismatchService = new PaymentRequestEventDeadLetterRecoveryService(
    mismatchStore,
    mismatchLedger,
    new PaymentRequestEventDeadLetterReplayPolicy());

var mismatch = await mismatchService.RecoverAsync(Request(eventId));
Assert(mismatch.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.AttemptHistoryMismatch, "Mismatched Attempt Ledger must fail closed.");
Assert(mismatchStore.TryRequeueCalls == 0, "Attempt-history mismatch must never call TryRequeueAsync.");

var limitedStore = new RecordingRecoveryStore(DeadLetter(eventId), tryRequeueResult: true);
var limitedLedger = new RecordingAttemptLedger(Attempts(eventId));
var limitedService = new PaymentRequestEventDeadLetterRecoveryService(
    limitedStore,
    limitedLedger,
    new PaymentRequestEventDeadLetterReplayPolicy(new(3)));

var limited = await limitedService.RecoverAsync(Request(eventId, priorReplayCount: 3));
Assert(limited.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.ReplayLimitExceeded, "Replay limit must be enforced.");
Assert(limitedStore.TryRequeueCalls == 0, "Replay-limit rejection must never call TryRequeueAsync.");

var conflictStore = new RecordingRecoveryStore(DeadLetter(eventId), tryRequeueResult: false);
var conflictLedger = new RecordingAttemptLedger(Attempts(eventId));
var conflictService = new PaymentRequestEventDeadLetterRecoveryService(
    conflictStore,
    conflictLedger,
    new PaymentRequestEventDeadLetterReplayPolicy());

var conflict = await conflictService.RecoverAsync(Request(eventId));
Assert(conflict.Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.RequeueConflict, "Optimistic requeue mismatch must surface as RequeueConflict.");
Assert(conflictStore.TryRequeueCalls == 1, "Approved plan may attempt one atomic requeue.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await approvedService.RecoverAsync(Request(eventId), cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}

Assert(approvedStore.GetCalls == 1, "Pre-cancelled recovery must not touch the store.");

Console.WriteLine("AFW-BE-REQUEST-OUTBOX-RECOVERY-1 application recovery orchestration scenarios: PASS");

sealed class RecordingRecoveryStore(
    PaymentRequestEventOutboxItem? item,
    bool tryRequeueResult)
    : IPaymentRequestEventDeadLetterRecoveryStore
{
    public int GetCalls { get; private set; }
    public int TryRequeueCalls { get; private set; }
    public PaymentRequestEventDeadLetterReplayPlan? LastPlan { get; private set; }

    public Task<PaymentRequestEventOutboxItem?> GetAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetCalls++;
        return Task.FromResult(item);
    }

    public Task<bool> TryRequeueAsync(
        PaymentRequestEventDeadLetterReplayPlan plan,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TryRequeueCalls++;
        LastPlan = plan;
        return Task.FromResult(tryRequeueResult);
    }
}

sealed class RecordingAttemptLedger(IReadOnlyList<PaymentRequestEventAttempt> attempts)
    : IPaymentRequestEventAttemptLedger
{
    public int ListCalls { get; private set; }

    public Task<Guid> BeginAttemptAsync(
        Guid eventId,
        int attemptNumber,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Recovery orchestration must never begin a dispatch attempt.");

    public Task CompleteAttemptAsync(
        Guid attemptId,
        PaymentRequestEventAttemptOutcome outcome,
        DateTimeOffset completedAtUtc,
        string? error = null,
        DateTimeOffset? nextAttemptAtUtc = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Recovery orchestration must never complete a dispatch attempt.");

    public Task<IReadOnlyList<PaymentRequestEventAttempt>> ListByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ListCalls++;
        return Task.FromResult(attempts);
    }
}
