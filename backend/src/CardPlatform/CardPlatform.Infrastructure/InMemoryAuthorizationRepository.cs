using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

public sealed class InMemoryAuthorizationRepository : ICardAuthorizationRepository
{
    private readonly List<CardAuthorization> _authorizations = [];

    public Task<CardAuthorization> CreateAsync(CardAuthorization authorization, CancellationToken cancellationToken = default)
    {
        _authorizations.Add(authorization);
        return Task.FromResult(authorization);
    }

    public Task<CardAuthorization?> GetByIdAsync(string authorizationId, CancellationToken cancellationToken = default)
    {
        var authorization = _authorizations.FirstOrDefault(a => a.AuthorizationId.Equals(authorizationId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(authorization);
    }
}
