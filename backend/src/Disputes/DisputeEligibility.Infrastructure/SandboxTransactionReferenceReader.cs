using AfriWallet.Disputes.Eligibility.Application.Abstractions;

namespace AfriWallet.Disputes.Eligibility.Infrastructure;

public sealed class SandboxTransactionReferenceReader : ITransactionReferenceReader
{
    private readonly Dictionary<Guid, TransactionReferenceSnapshot> items = new();

    public void Set(TransactionReferenceSnapshot transaction) => items[transaction.TransactionId] = transaction;

    public Task<TransactionReferenceSnapshot?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(transactionId, out var transaction);
        return Task.FromResult(transaction);
    }
}
