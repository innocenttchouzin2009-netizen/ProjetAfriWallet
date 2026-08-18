using AfriWallet.Disputes.Intelligence.Application.Models;

namespace AfriWallet.Disputes.Intelligence.Application.Abstractions;

public interface IDisputeIntelligenceSource
{
    Task<DisputeIntelligenceSnapshot?> GetAsync(string subjectId, CancellationToken cancellationToken = default);
}
