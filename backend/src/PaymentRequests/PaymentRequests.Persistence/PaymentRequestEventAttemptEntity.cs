namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestEventAttemptEntity
{
    public Guid AttemptId { get; set; }
    public Guid EventId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? Outcome { get; set; }
    public string? Error { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
}
