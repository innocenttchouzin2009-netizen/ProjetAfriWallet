namespace AfriWallet.Disputes.Decision.Application.Abstractions;

public sealed record InvestigationOutcomeSnapshot(
    Guid InvestigationId,
    Guid ClaimId,
    string Awid,
    string Status,
    string Outcome,
    string Classification,
    decimal DisputedAmount,
    string Currency,
    DateTimeOffset CompletedAtUtc);

public interface IInvestigationOutcomeReader
{
    Task<InvestigationOutcomeSnapshot?> GetAsync(Guid investigationId, CancellationToken cancellationToken = default);
}
