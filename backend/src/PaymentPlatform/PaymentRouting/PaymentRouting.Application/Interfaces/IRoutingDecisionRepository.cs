using PaymentRouting.Domain.Decisions;

namespace PaymentRouting.Application.Interfaces;

public interface IRoutingDecisionRepository
{
    Task AddAsync(
        RoutingDecision decision,
        CancellationToken cancellationToken);

    Task<RoutingDecision?> GetByPaymentIntentAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken);
}
