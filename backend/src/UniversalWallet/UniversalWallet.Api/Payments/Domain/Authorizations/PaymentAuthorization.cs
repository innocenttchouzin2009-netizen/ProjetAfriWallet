namespace UniversalWallet.Api.Payments.Domain.Authorizations;

public enum PaymentAuthorizationDecision
{
    Approved,
    Declined,
    ReviewRequired
}

public sealed class PaymentAuthorization
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PaymentIntentId { get; init; }
    public PaymentAuthorizationDecision Decision { get; init; }
    public string DecisionCode { get; init; } = string.Empty;
    public long AuthorizedAmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public Guid? ReservationId { get; init; }
    public int RiskScore { get; init; }
    public string RulesVersion { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; }
    public string AuthorizedBy { get; init; } = "backend";
}
