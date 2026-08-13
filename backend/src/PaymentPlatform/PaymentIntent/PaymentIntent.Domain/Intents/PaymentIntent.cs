using PaymentIntent.Domain.Methods;
using PaymentIntent.Domain.Money;

namespace PaymentIntent.Domain.Intents;

public sealed class PaymentIntent
{
    public PaymentIntent(
        Guid paymentIntentId,
        string reference,
        string payerId,
        string payeeId,
        MoneyAmount amount,
        PaymentMethodType paymentMethod,
        string idempotencyKey,
        DateTime expiresAtUtc)
    {
        if (paymentIntentId == Guid.Empty)
            throw new ArgumentException(
                "Payment intent ID is required.");

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "Expiration time must be in the future.");

        PaymentIntentId = paymentIntentId;
        Reference = Require(reference);
        PayerId = Require(payerId);
        PayeeId = Require(payeeId);
        Amount = amount;
        PaymentMethod = paymentMethod;
        IdempotencyKey = Require(idempotencyKey);
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid PaymentIntentId { get; }

    public string Reference { get; }

    public string PayerId { get; }

    public string PayeeId { get; }

    public MoneyAmount Amount { get; }

    public PaymentMethodType PaymentMethod { get; private set; }

    public string IdempotencyKey { get; }

    public PaymentIntentStatus Status { get; private set; }
        = PaymentIntentStatus.Created;

    public string? FailureCode { get; private set; }

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; }

    public DateTime? AuthorizedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public bool IsExpired(DateTime nowUtc) =>
        nowUtc >= ExpiresAtUtc;

    public void Authorize()
    {
        EnsureNotExpired();

        if (Status != PaymentIntentStatus.Created)
            throw new InvalidOperationException(
                "Only created payment intents can be authorized.");

        Status = PaymentIntentStatus.Authorized;
        AuthorizedAtUtc = DateTime.UtcNow;
    }

    public void StartProcessing()
    {
        EnsureNotExpired();

        if (Status != PaymentIntentStatus.Authorized)
            throw new InvalidOperationException(
                "Only authorized payment intents can start processing.");

        Status = PaymentIntentStatus.Processing;
    }

    public void Complete()
    {
        if (Status != PaymentIntentStatus.Processing)
            throw new InvalidOperationException(
                "Only processing payment intents can complete.");

        Status = PaymentIntentStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail(string failureCode)
    {
        if (Status is PaymentIntentStatus.Completed or
            PaymentIntentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Finalized payment intent cannot fail.");
        }

        FailureCode = Require(failureCode);
        Status = PaymentIntentStatus.Failed;
    }

    public void Cancel()
    {
        if (Status is PaymentIntentStatus.Completed or
            PaymentIntentStatus.Failed)
        {
            throw new InvalidOperationException(
                "Finalized payment intent cannot be cancelled.");
        }

        Status = PaymentIntentStatus.Cancelled;
    }

    public void Expire()
    {
        if (!IsExpired(DateTime.UtcNow))
            throw new InvalidOperationException(
                "Payment intent has not expired yet.");

        if (Status is PaymentIntentStatus.Completed or
            PaymentIntentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Finalized payment intent cannot expire.");
        }

        Status = PaymentIntentStatus.Expired;
    }

    private void EnsureNotExpired()
    {
        if (IsExpired(DateTime.UtcNow))
            throw new InvalidOperationException(
                "Payment intent has expired.");
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.");

        return value.Trim();
    }
}

public enum PaymentIntentStatus
{
    Created,
    Authorized,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Expired
}
