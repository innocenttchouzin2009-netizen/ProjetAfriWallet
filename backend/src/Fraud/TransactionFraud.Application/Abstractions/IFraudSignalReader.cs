using AfriWallet.Fraud.TransactionFraud.Domain.Signals;

namespace AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

public interface IFraudSignalReader
{
    Task<IReadOnlyCollection<FraudSignalSnapshot>> GetBySubjectAsync(
        string subjectType,
        string subjectId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}
