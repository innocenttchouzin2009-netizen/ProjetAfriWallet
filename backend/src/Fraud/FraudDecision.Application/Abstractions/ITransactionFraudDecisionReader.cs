using AfriWallet.Fraud.Decision.Domain.Inputs;

namespace AfriWallet.Fraud.Decision.Application.Abstractions;

public interface ITransactionFraudDecisionReader
{
    Task<TransactionFraudInput?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}