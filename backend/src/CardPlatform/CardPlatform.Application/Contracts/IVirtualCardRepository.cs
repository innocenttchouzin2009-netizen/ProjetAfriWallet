using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Contracts;

public interface IVirtualCardRepository
{
    Task<IReadOnlyList<VirtualCard>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VirtualCard?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default);
    Task<VirtualCard?> FindByTokenAsync(string cardToken, CancellationToken cancellationToken = default);
    Task<VirtualCard> CreateAsync(VirtualCard card, CancellationToken cancellationToken = default);
    Task<VirtualCard?> UpdateAsync(VirtualCard card, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default);
}
