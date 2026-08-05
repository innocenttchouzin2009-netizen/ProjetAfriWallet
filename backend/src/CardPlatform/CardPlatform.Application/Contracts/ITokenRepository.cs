using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Contracts;

public interface ITokenRepository
{
    Task<CardToken> CreateAsync(CardToken token, CancellationToken cancellationToken = default);
    Task<CardToken?> GetByIdAsync(string tokenId, CancellationToken cancellationToken = default);
    Task<CardToken?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<CardToken?> UpdateAsync(CardToken token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CardToken>> GetTokensForCardAsync(string cardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAuditTrailAsync(string tokenId, CancellationToken cancellationToken = default);
}
