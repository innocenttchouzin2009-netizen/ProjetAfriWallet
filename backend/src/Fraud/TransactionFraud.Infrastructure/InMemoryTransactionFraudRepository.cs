using System.Collections.Concurrent;
using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Domain.Detection;

namespace AfriWallet.Fraud.TransactionFraud.Infrastructure;

public sealed class InMemoryTransactionFraudRepository : ITransactionFraudRepository
{
    private readonly ConcurrentDictionary<Guid, TransactionFraudDetection> _detections = new();

    public Task SaveAsync(TransactionFraudDetection detection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _detections[detection.TransactionId] = detection;
        return Task.CompletedTask;
    }

    public Task<TransactionFraudDetection?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _detections.TryGetValue(transactionId, out var detection);
        return Task.FromResult(detection);
    }
}
