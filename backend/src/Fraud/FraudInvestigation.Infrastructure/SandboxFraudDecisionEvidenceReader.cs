using AfriWallet.Fraud.Investigation.Application.Abstractions;

namespace AfriWallet.Fraud.Investigation.Infrastructure;

public sealed class SandboxFraudDecisionEvidenceReader : IFraudDecisionEvidenceReader
{
    private readonly Dictionary<Guid, FraudDecisionEvidenceSnapshot> items = new();

    public void Set(FraudDecisionEvidenceSnapshot snapshot) => items[snapshot.TransactionId] = snapshot;

    public Task<FraudDecisionEvidenceSnapshot?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(transactionId, out var snapshot);
        return Task.FromResult(snapshot);
    }
}