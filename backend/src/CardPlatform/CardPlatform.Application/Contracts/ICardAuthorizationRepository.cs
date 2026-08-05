using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Contracts;

public interface ICardAuthorizationRepository
{
    Task<CardAuthorization> CreateAsync(CardAuthorization authorization, CancellationToken cancellationToken = default);
    Task<CardAuthorization?> GetByIdAsync(string authorizationId, CancellationToken cancellationToken = default);
}
