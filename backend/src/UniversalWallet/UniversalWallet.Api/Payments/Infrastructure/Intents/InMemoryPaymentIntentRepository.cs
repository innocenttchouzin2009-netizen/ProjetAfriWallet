using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Payments.Infrastructure.Intents;

public sealed class InMemoryPaymentIntentRepository : IPaymentIntentRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentIntent> _intents = new();
    private readonly Dictionary<string, Guid> _byIdempotency = new(StringComparer.OrdinalIgnoreCase);

    public Task<PaymentIntent?> GetAsync(Guid intentId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult(_intents.TryGetValue(intentId, out var intent) ? intent : null);
        }
    }

    public Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid payerAwid, string idempotencyKey, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_byIdempotency.TryGetValue(BuildKey(payerAwid, idempotencyKey), out var intentId))
            {
                return Task.FromResult<PaymentIntent?>(null);
            }

            return Task.FromResult(_intents.TryGetValue(intentId, out var intent) ? intent : null);
        }
    }

    public Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _intents[intent.Id] = intent;
            _byIdempotency[BuildKey(intent.PayerAwid, intent.IdempotencyKey)] = intent.Id;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<PaymentIntent>> ListAsync(Guid payerAwid, PaymentIntentStatus? status, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var values = _intents.Values
                .Where(intent => payerAwid == Guid.Empty || intent.PayerAwid == payerAwid)
                .Where(intent => status is null || intent.Status == status)
                .OrderByDescending(intent => intent.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<PaymentIntent>>(values);
        }
    }

    private static string BuildKey(Guid payerAwid, string idempotencyKey) => $"{payerAwid:N}:{idempotencyKey.Trim().ToLowerInvariant()}";
}
