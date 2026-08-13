using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Domain.Decisions;

public sealed record RoutingDecision(
    Guid DecisionId,
    Guid PaymentIntentId,
    PaymentRoute SelectedRoute,
    IReadOnlyCollection<PaymentRoute> Alternatives,
    string Reason,
    DateTime CreatedAtUtc);
