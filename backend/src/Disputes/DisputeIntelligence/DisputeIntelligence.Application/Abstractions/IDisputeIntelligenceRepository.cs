using AfriWallet.Disputes.Intelligence.Domain.Findings;

namespace AfriWallet.Disputes.Intelligence.Application.Abstractions;

public interface IDisputeIntelligenceRepository
{
    Task SaveAsync(ProtectionFinding finding, CancellationToken cancellationToken = default);
    Task<ProtectionFinding?> GetLatestAsync(string subjectId, CancellationToken cancellationToken = default);
}
