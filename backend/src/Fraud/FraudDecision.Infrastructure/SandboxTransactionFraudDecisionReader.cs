using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Domain.Inputs;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class SandboxTransactionFraudDecisionReader : ITransactionFraudDecisionReader
{
    private readonly Dictionary<Guid, TransactionFraudInput> items = new();

    public void Set(TransactionFraudInput input) => items[input.TransactionId] = input;

    public Task<TransactionFraudInput?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(transactionId, out var input);
        return Task.FromResult(input);
    }
}