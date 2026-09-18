namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestEventDeadLetterRecoveryExecutionCode
{
    Requeued = 1,
    EventNotFound = 2,
    NotDeadLetter = 3,
    AttemptHistoryMismatch = 4,
    ReplayLimitExceeded = 5,
    RequeueConflict = 6
}

public sealed record PaymentRequestEventDeadLetterRecoveryExecutionResult(
    PaymentRequestEventDeadLetterRecoveryExecutionCode Code,
    PaymentRequestEventDeadLetterReplayDecision? Decision)
{
    public bool Requeued => Code == PaymentRequestEventDeadLetterRecoveryExecutionCode.Requeued;

    public static PaymentRequestEventDeadLetterRecoveryExecutionResult EventNotFound() =>
        new(PaymentRequestEventDeadLetterRecoveryExecutionCode.EventNotFound, null);

    public static PaymentRequestEventDeadLetterRecoveryExecutionResult FromDecision(
        PaymentRequestEventDeadLetterReplayDecision decision) =>
        new(decision.Code switch
        {
            PaymentRequestEventDeadLetterReplayDecisionCode.NotDeadLetter =>
                PaymentRequestEventDeadLetterRecoveryExecutionCode.NotDeadLetter,
            PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch =>
                PaymentRequestEventDeadLetterRecoveryExecutionCode.AttemptHistoryMismatch,
            PaymentRequestEventDeadLetterReplayDecisionCode.ReplayLimitExceeded =>
                PaymentRequestEventDeadLetterRecoveryExecutionCode.ReplayLimitExceeded,
            PaymentRequestEventDeadLetterReplayDecisionCode.Approved =>
                PaymentRequestEventDeadLetterRecoveryExecutionCode.RequeueConflict,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        }, decision);
}

public sealed class PaymentRequestEventDeadLetterRecoveryService(
    IPaymentRequestEventDeadLetterRecoveryStore recoveryStore,
    IPaymentRequestEventAttemptLedger attemptLedger,
    PaymentRequestEventDeadLetterReplayPolicy replayPolicy)
{
    public async Task<PaymentRequestEventDeadLetterRecoveryExecutionResult> RecoverAsync(
        PaymentRequestEventDeadLetterReplayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(request));
        }

        var item = await recoveryStore.GetAsync(request.EventId, cancellationToken);
        if (item is null)
        {
            return PaymentRequestEventDeadLetterRecoveryExecutionResult.EventNotFound();
        }

        var attempts = await attemptLedger.ListByEventAsync(request.EventId, cancellationToken);
        var decision = replayPolicy.Evaluate(request, item, attempts);

        if (!decision.Approved || decision.Plan is null)
        {
            return PaymentRequestEventDeadLetterRecoveryExecutionResult.FromDecision(decision);
        }

        var requeued = await recoveryStore.TryRequeueAsync(decision.Plan, cancellationToken);
        return requeued
            ? new(
                PaymentRequestEventDeadLetterRecoveryExecutionCode.Requeued,
                decision)
            : new(
                PaymentRequestEventDeadLetterRecoveryExecutionCode.RequeueConflict,
                decision);
    }
}
