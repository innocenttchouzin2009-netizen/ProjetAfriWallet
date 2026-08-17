namespace AfriWallet.Fraud.Investigation.Application.Abstractions;

public sealed record FraudDecisionEvidenceSnapshot(Guid DecisionId, Guid TransactionId, string Awid, int Score, string Band, string Action, DateTimeOffset DecidedAtUtc);

public interface IFraudDecisionEvidenceReader
{
    Task<FraudDecisionEvidenceSnapshot?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}