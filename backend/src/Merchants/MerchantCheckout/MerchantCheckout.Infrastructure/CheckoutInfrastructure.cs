using System.Collections.Concurrent;
using AfriWallet.Merchants.Checkout.Application.Abstractions;
using AfriWallet.Merchants.Checkout.Domain.Checkout;
using AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

namespace AfriWallet.Merchants.Checkout.Infrastructure;

public sealed class SandboxMerchantCommerceEligibilityReader : IMerchantCommerceEligibilityReader
{
    private readonly Dictionary<string, MerchantCommerceEligibilitySnapshot> _items = new(StringComparer.OrdinalIgnoreCase);
    public void Set(MerchantCommerceEligibilitySnapshot snapshot) => _items[snapshot.MerchantId] = snapshot;
    public Task<MerchantCommerceEligibilitySnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); _items.TryGetValue(merchantId, out var result); return Task.FromResult(result); }
}
public sealed class InMemoryCheckoutSessionRepository : ICheckoutSessionRepository
{
    private readonly ConcurrentDictionary<Guid, CheckoutSession> _items = new();
    public Task AddAsync(CheckoutSession session, CancellationToken cancellationToken = default) { if (!_items.TryAdd(session.CheckoutSessionId, session)) throw new InvalidOperationException("Checkout session already exists."); return Task.CompletedTask; }
    public Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default) { _items[session.CheckoutSessionId] = session; return Task.CompletedTask; }
    public Task<CheckoutSession?> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default) { _items.TryGetValue(checkoutSessionId, out var result); return Task.FromResult(result); }
    public Task<CheckoutSession?> GetByMerchantOrderReferenceAsync(string merchantId, string merchantOrderReference, CancellationToken cancellationToken = default) => Task.FromResult(_items.Values.FirstOrDefault(x => string.Equals(x.MerchantId, merchantId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.MerchantOrderReference, merchantOrderReference, StringComparison.OrdinalIgnoreCase)));
}
public sealed class InMemoryPaymentIntentRepository : IPaymentIntentRepository
{
    private readonly ConcurrentDictionary<Guid, PaymentIntent> _items = new();
    public Task AddAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken = default) { if (!_items.TryAdd(paymentIntent.PaymentIntentId, paymentIntent)) throw new InvalidOperationException("Payment intent already exists."); return Task.CompletedTask; }
    public Task SaveAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken = default) { _items[paymentIntent.PaymentIntentId] = paymentIntent; return Task.CompletedTask; }
    public Task<PaymentIntent?> GetAsync(Guid paymentIntentId, CancellationToken cancellationToken = default) { _items.TryGetValue(paymentIntentId, out var result); return Task.FromResult(result); }
    public Task<PaymentIntent?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) => Task.FromResult(_items.Values.FirstOrDefault(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));
}
public sealed class InMemoryCheckoutAuditStore : ICheckoutAuditStore
{
    private readonly ConcurrentQueue<CheckoutAuditEvent> _events = new();
    public Task AppendAsync(CheckoutAuditEvent auditEvent, CancellationToken cancellationToken = default) { _events.Enqueue(auditEvent); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<CheckoutAuditEvent>> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default) { IReadOnlyCollection<CheckoutAuditEvent> result = _events.Where(x => x.CheckoutSessionId == checkoutSessionId).ToArray(); return Task.FromResult(result); }
}
public sealed class SystemCheckoutClock : ICheckoutClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
