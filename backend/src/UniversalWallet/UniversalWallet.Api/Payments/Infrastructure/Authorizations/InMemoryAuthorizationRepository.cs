using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Domain.Authorizations;

namespace UniversalWallet.Api.Payments.Infrastructure.Authorizations;

public sealed class InMemoryAuthorizationRepository : IPaymentAuthorizationRepository
{
    private readonly Dictionary<Guid, PaymentAuthorization> _authorizations = new();
    private readonly Dictionary<Guid, Guid> _byIntent = new();

    public Task<PaymentAuthorization?> GetAsync(Guid authorizationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_authorizations.GetValueOrDefault(authorizationId));
    }

    public Task<PaymentAuthorization?> GetByIntentAsync(Guid intentId, CancellationToken cancellationToken = default)
    {
        if (_byIntent.TryGetValue(intentId, out var authorizationId))
        {
            return Task.FromResult<PaymentAuthorization?>(_authorizations[authorizationId]);
        }

        return Task.FromResult<PaymentAuthorization?>(null);
    }

    public Task AddAsync(PaymentAuthorization authorization, CancellationToken cancellationToken = default)
    {
        _authorizations[authorization.Id] = authorization;
        _byIntent[authorization.PaymentIntentId] = authorization.Id;
        return Task.CompletedTask;
    }
}
