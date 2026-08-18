using AfriWallet.Disputes.Decision.Application.Abstractions;

namespace AfriWallet.Disputes.Decision.Infrastructure;

public sealed class SandboxInvestigationOutcomeReader : IInvestigationOutcomeReader
{
    private readonly Dictionary<Guid, InvestigationOutcomeSnapshot> items = new();

    public void Set(InvestigationOutcomeSnapshot snapshot) => items[snapshot.InvestigationId] = snapshot;

    public Task<InvestigationOutcomeSnapshot?> GetAsync(Guid investigationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(investigationId, out var result);
        return Task.FromResult(result);
    }
}
