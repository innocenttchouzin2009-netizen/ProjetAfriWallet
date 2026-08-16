using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

public interface ITransactionHistoryRepository
{
    Task AddAsync(
        MonitoredTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MonitoredTransaction>> GetByAwidAsync(
        string awid,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}