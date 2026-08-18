using AfriWallet.Merchants.Checkout.Domain.Checkout;
using AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

namespace AfriWallet.Merchants.Checkout.Application.Abstractions;

public sealed record MerchantCommerceEligibilitySnapshot(string MerchantId, string RegistryStatus, string VerificationStatus, string CountryCode, string SettlementCurrency);
public interface IMerchantCommerceEligibilityReader { Task<MerchantCommerceEligibilitySnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default); }
public interface ICheckoutSessionRepository { Task AddAsync(CheckoutSession session, CancellationToken cancellationToken = default); Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default); Task<CheckoutSession?> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default); Task<CheckoutSession?> GetByMerchantOrderReferenceAsync(string merchantId, string merchantOrderReference, CancellationToken cancellationToken = default); }
public interface IPaymentIntentRepository { Task AddAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken = default); Task SaveAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken = default); Task<PaymentIntent?> GetAsync(Guid paymentIntentId, CancellationToken cancellationToken = default); Task<PaymentIntent?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default); }
public sealed record CheckoutAuditEvent(Guid EventId, Guid CheckoutSessionId, Guid? PaymentIntentId, string MerchantId, string EventType, string Actor, DateTimeOffset OccurredAtUtc, IReadOnlyDictionary<string, string> Metadata);
public interface ICheckoutAuditStore { Task AppendAsync(CheckoutAuditEvent auditEvent, CancellationToken cancellationToken = default); Task<IReadOnlyCollection<CheckoutAuditEvent>> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default); }
public interface ICheckoutClock { DateTimeOffset UtcNow { get; } }
