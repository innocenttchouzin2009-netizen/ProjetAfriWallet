using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<T>(Action action, string message) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 18, 17, 30, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var requestId = PaymentRequestId.From(Guid.NewGuid());
var envelope = new PaymentRequestEventEnvelope(eventId, requestId, "payment_request.paid", now.AddMinutes(-10), "{}");
var dead = new PaymentRequestEventOutboxItem(
    envelope,
    PaymentRequestEventOutboxStatus.DeadLetter,
    5,
    now.AddMinutes(-10),
    now.AddMinutes(-1),
    null,
    now.AddMinutes(-1),
    null,
    "Provider rejected delivery.");

var attempts = Enumerable.Range(1, 5)
    .Select(n => new PaymentRequestEventAttempt(
        Guid.NewGuid(),
        eventId,
        n,
        now.AddMinutes(-10 + n),
        now.AddMinutes(-10 + n).AddSeconds(1),
        n == 5 ? PaymentRequestEventAttemptOutcome.DeadLetter : PaymentRequestEventAttemptOutcome.RetryScheduled,
        "failure",
        n == 5 ? null : now.AddMinutes(-9 + n)))
    .ToArray();

var policy = new PaymentRequestEventDeadLetterReplayPolicy(new(3));
var decision = policy.Evaluate(
    new PaymentRequestEventDeadLetterReplayRequest(
        eventId,
        " ops.user ",
        " provider configuration corrected ",
        now,
        now.AddMinutes(2),
        0),
    dead,
    attempts);

Assert(decision.Approved, "Valid dead-letter replay must be approved.");
Assert(decision.Plan!.ExpectedAttemptCount == 5, "Replay must preserve the current attempt count.");
Assert(decision.Plan.ReplayOrdinal == 1, "First controlled replay must use ordinal 1.");
Assert(decision.Plan.RequestedBy == "ops.user", "Actor must be normalized.");
Assert(decision.Plan.Reason == "provider configuration corrected", "Reason must be normalized.");
Assert(decision.Plan.AvailableAtUtc == now.AddMinutes(2), "Availability must be preserved.");

var notDead = dead with { Status = PaymentRequestEventOutboxStatus.Retry };
Assert(
    policy.Evaluate(
        new(eventId, "ops", "retry", now, now, 0),
        notDead,
        attempts).Code == PaymentRequestEventDeadLetterReplayDecisionCode.NotDeadLetter,
    "Only DeadLetter items may be replayed.");

var mismatchedAttempts = attempts[..^1];
Assert(
    policy.Evaluate(
        new(eventId, "ops", "retry", now, now, 0),
        dead,
        mismatchedAttempts).Code == PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch,
    "Replay must fail closed when the latest attempt ledger entry does not match the outbox attempt count.");

var wrongOutcome = attempts[..^1]
    .Append(attempts[^1] with { Outcome = PaymentRequestEventAttemptOutcome.RetryScheduled, NextAttemptAtUtc = now.AddMinutes(1) })
    .ToArray();
Assert(
    policy.Evaluate(
        new(eventId, "ops", "retry", now, now, 0),
        dead,
        wrongOutcome).Code == PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch,
    "Latest attempt must itself be DeadLetter.");

Assert(
    policy.Evaluate(
        new(eventId, "ops", "retry", now, now, 3),
        dead,
        attempts).Code == PaymentRequestEventDeadLetterReplayDecisionCode.ReplayLimitExceeded,
    "Replay limit must be enforced.");

AssertThrows<ArgumentException>(
    () => policy.Evaluate(new(eventId, "", "reason", now, now, 0), dead, attempts),
    "Replay actor is required.");
AssertThrows<ArgumentException>(
    () => policy.Evaluate(new(eventId, "ops", "", now, now, 0), dead, attempts),
    "Replay reason is required.");
AssertThrows<ArgumentException>(
    () => policy.Evaluate(new(eventId, "ops", "reason", now, now.AddMinutes(-1), 0), dead, attempts),
    "Replay availability cannot precede request time.");
AssertThrows<ArgumentException>(
    () => policy.Evaluate(new(eventId, "ops", "reason", now.ToOffset(TimeSpan.FromHours(2)), now, 0), dead, attempts),
    "Replay request timestamp must be UTC.");
AssertThrows<ArgumentException>(
    () => policy.Evaluate(new(Guid.NewGuid(), "ops", "reason", now, now, 0), dead, attempts),
    "Replay event id must match the outbox item.");

var noFailureEvidence = dead with { LastError = null };
Assert(
    policy.Evaluate(new(eventId, "ops", "reason", now, now, 0), noFailureEvidence, attempts).Code ==
        PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch,
    "Dead-letter replay requires durable failure evidence.");

Console.WriteLine("AFW-BE-REQUEST-OUTBOX-RECOVERY-1 dead-letter recovery contracts and replay policy scenarios: PASS");
