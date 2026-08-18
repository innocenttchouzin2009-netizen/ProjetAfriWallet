namespace AfriWallet.Disputes.Eligibility.Application.Abstractions;

public sealed record TransactionReferenceSnapshot(
    Guid TransactionId,
    string Awid,
    long AmountMinor,
    string Currency,
    string Status,
    DateTimeOffset OccurredAtUtc);

public interface ITransactionReferenceReader
{
    Task<TransactionReferenceSnapshot?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
