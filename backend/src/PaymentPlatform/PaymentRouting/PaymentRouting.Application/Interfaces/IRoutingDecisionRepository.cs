using PaymentRouting.Domain.Decisions;

namespace PaymentRouting.Application.Interfaces;

public sealed class RoutingDecisionConflictException(Guid paymentIntentId)
    : InvalidOperationException($"A routing decision already exists for payment intent {paymentIntentId:D}.")
{
    public Guid PaymentIntentId { get; } = paymentIntentId;
}

public interface IRoutingDecisionRepository
{
    Task AddAsync(
        RoutingDecision decision,
        CancellationToken cancellationToken);

    Task<RoutingDecision?> GetByPaymentIntentAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken);
}
