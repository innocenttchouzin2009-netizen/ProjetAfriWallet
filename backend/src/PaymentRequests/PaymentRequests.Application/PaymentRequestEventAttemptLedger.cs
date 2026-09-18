namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestEventAttemptOutcome
{
    Delivered = 1,
    RetryScheduled = 2,
    DeadLetter = 3,
    Cancelled = 4
}

public sealed record PaymentRequestEventAttempt(
    Guid AttemptId,
    Guid EventId,
    int AttemptNumber,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    PaymentRequestEventAttemptOutcome? Outcome,
    string? Error,
    DateTimeOffset? NextAttemptAtUtc);

public interface IPaymentRequestEventAttemptLedger
{
    Task<Guid> BeginAttemptAsync(Guid eventId, int attemptNumber, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default);
    Task CompleteAttemptAsync(Guid attemptId, PaymentRequestEventAttemptOutcome outcome, DateTimeOffset completedAtUtc, string? error = null, DateTimeOffset? nextAttemptAtUtc = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentRequestEventAttempt>> ListByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public sealed class InMemoryPaymentRequestEventAttemptLedger : IPaymentRequestEventAttemptLedger
{
    private readonly Dictionary<(Guid EventId, int AttemptNumber), PaymentRequestEventAttempt> attempts = new();

    public Task<Guid> BeginAttemptAsync(Guid eventId, int attemptNumber, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateBegin(eventId, attemptNumber, startedAtUtc);
        var key = (eventId, attemptNumber);
        if (attempts.TryGetValue(key, out var existing)) return Task.FromResult(existing.AttemptId);
        var attempt = new PaymentRequestEventAttempt(Guid.NewGuid(), eventId, attemptNumber, startedAtUtc, null, null, null, null);
        attempts.Add(key, attempt);
        return Task.FromResult(attempt.AttemptId);
    }

    public Task CompleteAttemptAsync(Guid attemptId, PaymentRequestEventAttemptOutcome outcome, DateTimeOffset completedAtUtc, string? error = null, DateTimeOffset? nextAttemptAtUtc = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompletion(attemptId, outcome, completedAtUtc, nextAttemptAtUtc);
        var pair = attempts.FirstOrDefault(x => x.Value.AttemptId == attemptId);
        if (pair.Value is null) throw new InvalidOperationException("Attempt was not found.");
        if (pair.Value.CompletedAtUtc is not null) return Task.CompletedTask;
        attempts[pair.Key] = pair.Value with { CompletedAtUtc = completedAtUtc, Outcome = outcome, Error = NormalizeError(error), NextAttemptAtUtc = nextAttemptAtUtc };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentRequestEventAttempt>> ListByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        IReadOnlyList<PaymentRequestEventAttempt> result = attempts.Values.Where(x => x.EventId == eventId).OrderBy(x => x.AttemptNumber).ToArray();
        return Task.FromResult(result);
    }

    public static void ValidateBegin(Guid eventId, int attemptNumber, DateTimeOffset startedAtUtc)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        if (attemptNumber <= 0) throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        EnsureUtc(startedAtUtc, nameof(startedAtUtc));
    }

    public static void ValidateCompletion(Guid attemptId, PaymentRequestEventAttemptOutcome outcome, DateTimeOffset completedAtUtc, DateTimeOffset? nextAttemptAtUtc)
    {
        if (attemptId == Guid.Empty) throw new ArgumentException("Attempt id cannot be empty.", nameof(attemptId));
        if (!Enum.IsDefined(outcome)) throw new ArgumentOutOfRangeException(nameof(outcome));
        EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        if (nextAttemptAtUtc is not null)
        {
            EnsureUtc(nextAttemptAtUtc.Value, nameof(nextAttemptAtUtc));
            if (nextAttemptAtUtc.Value < completedAtUtc) throw new ArgumentException("Next attempt cannot precede completion.", nameof(nextAttemptAtUtc));
        }
        if (outcome == PaymentRequestEventAttemptOutcome.RetryScheduled && nextAttemptAtUtc is null) throw new ArgumentException("Retry outcome requires next attempt time.", nameof(nextAttemptAtUtc));
        if (outcome != PaymentRequestEventAttemptOutcome.RetryScheduled && nextAttemptAtUtc is not null) throw new ArgumentException("Only retry outcome may carry next attempt time.", nameof(nextAttemptAtUtc));
    }

    public static string? NormalizeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return null;
        var value = error.Trim();
        return value.Length <= 2048 ? value : value[..2048];
    }

    public static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
