using AfriWallet.Fraud.Decision.Domain.Decisions;

namespace AfriWallet.Fraud.Decision.Application.Abstractions;

public interface IFraudDecisionRepository
{
    Task SaveAsync(FraudDecision decision, CancellationToken cancellationToken = default);
    Task<FraudDecision?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}