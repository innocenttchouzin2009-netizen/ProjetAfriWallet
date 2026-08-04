using UniversalWallet.Api.Payments.Application.MerchantPayments;
using UniversalWallet.Api.Payments.Domain.MerchantPayments;

namespace UniversalWallet.Api.Payments.Infrastructure.MerchantPayments;

public sealed class InMemoryMerchantProfileRepository : IMerchantProfileRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, MerchantProfile> _merchants = new();
    private readonly Dictionary<Guid, Guid> _byAwid = new();

    public Task<MerchantProfile?> GetAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_merchants.TryGetValue(merchantId, out var merchant) ? merchant : null);
        }
    }

    public Task<MerchantProfile?> GetByAwidAsync(Guid merchantAwid, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byAwid.TryGetValue(merchantAwid, out var merchantId) && _merchants.TryGetValue(merchantId, out var merchant) ? merchant : null);
        }
    }

    public Task AddAsync(MerchantProfile merchant, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _merchants[merchant.Id] = merchant;
            _byAwid[merchant.MerchantAwid] = merchant.Id;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(MerchantProfile merchant, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _merchants[merchant.Id] = merchant;
            _byAwid[merchant.MerchantAwid] = merchant.Id;
            return Task.CompletedTask;
        }
    }
}

public sealed class InMemoryMerchantPaymentRequestRepository : IMerchantPaymentRequestRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, MerchantPaymentRequest> _requests = new();
    private readonly Dictionary<Guid, Guid> _byQrToken = new();

    public Task<MerchantPaymentRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_requests.TryGetValue(requestId, out var request) ? request : null);
        }
    }

    public Task<MerchantPaymentRequest?> GetByQrTokenAsync(Guid qrTokenId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byQrToken.TryGetValue(qrTokenId, out var requestId) && _requests.TryGetValue(requestId, out var request) ? request : null);
        }
    }

    public Task AddAsync(MerchantPaymentRequest request, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _requests[request.Id] = request;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(MerchantPaymentRequest request, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _requests[request.Id] = request;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<MerchantPaymentRequest>> ListAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<MerchantPaymentRequest>>(_requests.Values.Where(r => r.MerchantId == merchantId).ToList());
        }
    }
}

public sealed class InMemoryMerchantQrTokenRepository : IMerchantQrTokenRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, MerchantQrToken> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public Task<MerchantQrToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_tokens.TryGetValue(token, out var qrToken) ? qrToken : null);
        }
    }

    public Task AddAsync(MerchantQrToken token, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _tokens[token.Token] = token;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(MerchantQrToken token, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _tokens[token.Token] = token;
            return Task.CompletedTask;
        }
    }
}
