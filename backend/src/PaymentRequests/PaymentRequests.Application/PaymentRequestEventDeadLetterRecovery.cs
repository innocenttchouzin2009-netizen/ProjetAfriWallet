namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestEventDeadLetterReplayOptions(int MaxReplayCount)
{
    public static PaymentRequestEventDeadLetterReplayOptions Default { get; } = new(3);
}

public sealed record PaymentRequestEventDeadLetterReplayRequest(
    Guid EventId,
    string RequestedBy,
    string Reason,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset AvailableAtUtc,
    int PriorReplayCount);

public enum PaymentRequestEventDeadLetterReplayDecisionCode
{
    Approved = 1,
    NotDeadLetter = 2,
    AttemptHistoryMismatch = 3,
    ReplayLimitExceeded = 4
}

public sealed record PaymentRequestEventDeadLetterReplayPlan(
    Guid EventId,
    int ExpectedAttemptCount,
    int ReplayOrdinal,
    DateTimeOffset AvailableAtUtc,
    string RequestedBy,
    string Reason);

public sealed record PaymentRequestEventDeadLetterReplayDecision(
    PaymentRequestEventDeadLetterReplayDecisionCode Code,
    PaymentRequestEventDeadLetterReplayPlan? Plan)
{
    public bool Approved => Code == PaymentRequestEventDeadLetterReplayDecisionCode.Approved && Plan is not null;

    public static PaymentRequestEventDeadLetterReplayDecision Allow(PaymentRequestEventDeadLetterReplayPlan plan) =>
        new(PaymentRequestEventDeadLetterReplayDecisionCode.Approved, plan);

    public static PaymentRequestEventDeadLetterReplayDecision Reject(PaymentRequestEventDeadLetterReplayDecisionCode code) =>
        new(code, null);
}

public interface IPaymentRequestEventDeadLetterRecoveryStore
{
    Task<PaymentRequestEventOutboxItem?> GetAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<bool> TryRequeueAsync(
        PaymentRequestEventDeadLetterReplayPlan plan,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentRequestEventDeadLetterReplayPolicy
{
    private readonly PaymentRequestEventDeadLetterReplayOptions options;

    public PaymentRequestEventDeadLetterReplayPolicy(PaymentRequestEventDeadLetterReplayOptions? options = null)
    {
        this.options = options ?? PaymentRequestEventDeadLetterReplayOptions.Default;
        if (this.options.MaxReplayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxReplayCount must be positive.");
        }
    }

    public PaymentRequestEventDeadLetterReplayDecision Evaluate(
        PaymentRequestEventDeadLetterReplayRequest request,
        PaymentRequestEventOutboxItem item,
        IReadOnlyList<PaymentRequestEventAttempt> attempts)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(attempts);

        ValidateRequest(request);

        if (item.Event.EventId != request.EventId)
        {
            throw new ArgumentException("Replay request event id must match the outbox item.", nameof(item));
        }

        if (item.Status != PaymentRequestEventOutboxStatus.DeadLetter)
        {
            return PaymentRequestEventDeadLetterReplayDecision.Reject(
                PaymentRequestEventDeadLetterReplayDecisionCode.NotDeadLetter);
        }

        if (request.PriorReplayCount >= options.MaxReplayCount)
        {
            return PaymentRequestEventDeadLetterReplayDecision.Reject(
                PaymentRequestEventDeadLetterReplayDecisionCode.ReplayLimitExceeded);
        }

        if (item.AttemptCount <= 0 ||
            item.LastAttemptAtUtc is null ||
            string.IsNullOrWhiteSpace(item.LastError))
        {
            return PaymentRequestEventDeadLetterReplayDecision.Reject(
                PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch);
        }

        var latest = attempts
            .Where(x => x.EventId == request.EventId)
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (latest is null ||
            latest.AttemptNumber != item.AttemptCount ||
            latest.CompletedAtUtc is null ||
            latest.Outcome != PaymentRequestEventAttemptOutcome.DeadLetter)
        {
            return PaymentRequestEventDeadLetterReplayDecision.Reject(
                PaymentRequestEventDeadLetterReplayDecisionCode.AttemptHistoryMismatch);
        }

        return PaymentRequestEventDeadLetterReplayDecision.Allow(
            new PaymentRequestEventDeadLetterReplayPlan(
                request.EventId,
                item.AttemptCount,
                request.PriorReplayCount + 1,
                request.AvailableAtUtc,
                NormalizeRequired(request.RequestedBy, 128, nameof(request.RequestedBy)),
                NormalizeRequired(request.Reason, 512, nameof(request.Reason))));
    }

    private static void ValidateRequest(PaymentRequestEventDeadLetterReplayRequest request)
    {
        if (request.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(request));
        }

        _ = NormalizeRequired(request.RequestedBy, 128, nameof(request.RequestedBy));
        _ = NormalizeRequired(request.Reason, 512, nameof(request.Reason));

        EnsureUtc(request.RequestedAtUtc, nameof(request.RequestedAtUtc));
        EnsureUtc(request.AvailableAtUtc, nameof(request.AvailableAtUtc));

        if (request.AvailableAtUtc < request.RequestedAtUtc)
        {
            throw new ArgumentException("Replay availability cannot precede the replay request.", nameof(request));
        }

        if (request.PriorReplayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Prior replay count cannot be negative.");
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
