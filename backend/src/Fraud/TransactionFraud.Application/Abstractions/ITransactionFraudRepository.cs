using AfriWallet.Fraud.TransactionFraud.Domain.Detection;

namespace AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

public interface ITransactionFraudRepository
{
    Task SaveAsync(TransactionFraudDetection detection, CancellationToken cancellationToken = default);
    Task<TransactionFraudDetection?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
