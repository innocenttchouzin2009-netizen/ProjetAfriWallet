using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Contracts;

public interface ICardLifecycleRepository
{
    Task<CardLifecycle> CreateAsync(CardLifecycle card, CancellationToken cancellationToken = default);
    Task<CardLifecycle?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default);
    Task<CardLifecycle?> UpdateAsync(CardLifecycle card, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTimelineAsync(string cardId, CancellationToken cancellationToken = default);
}
