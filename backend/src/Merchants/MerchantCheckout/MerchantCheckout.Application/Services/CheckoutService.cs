using AfriWallet.Merchants.Checkout.Application.Abstractions;
using AfriWallet.Merchants.Checkout.Application.Commands;
using AfriWallet.Merchants.Checkout.Application.Policies;
using AfriWallet.Merchants.Checkout.Application.Results;
using AfriWallet.Merchants.Checkout.Domain.Checkout;
using AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

namespace AfriWallet.Merchants.Checkout.Application.Services;

public sealed class CheckoutService(ICheckoutSessionRepository sessions, IPaymentIntentRepository intents, IMerchantCommerceEligibilityReader merchants, ICheckoutAuditStore audit, ICheckoutClock clock, CheckoutEligibilityPolicy eligibility)
{
    public async Task<CheckoutSessionResult> CreateAsync(CreateCheckoutSessionCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor) || string.IsNullOrWhiteSpace(command.MerchantId)) throw new ArgumentException("Merchant id and actor are required.", nameof(command));
        if (command.ExpiresInMinutes is < 1 or > 1440) throw new ArgumentOutOfRangeException(nameof(command.ExpiresInMinutes));
        if (string.IsNullOrWhiteSpace(command.PaymentIntentIdempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(command));
        var existingIntent = await intents.GetByIdempotencyKeyAsync(command.PaymentIntentIdempotencyKey, ct);
        if (existingIntent is not null) return Map(await RequiredSession(existingIntent.CheckoutSessionId, ct));
        if (!string.IsNullOrWhiteSpace(command.MerchantOrderReference)) { var existingOrder = await sessions.GetByMerchantOrderReferenceAsync(command.MerchantId, command.MerchantOrderReference, ct); if (existingOrder is not null) return Map(existingOrder); }
        var merchant = await merchants.GetAsync(command.MerchantId, ct) ?? throw new KeyNotFoundException("Merchant not found.");
        eligibility.EnsureEligible(merchant);
        var now = clock.UtcNow; var expiry = now.AddMinutes(command.ExpiresInMinutes);
        var session = new CheckoutSession(Guid.NewGuid(), command.MerchantId, command.CustomerReference, command.MerchantOrderReference, command.AmountMinor, command.Currency, command.ReturnUrl, new CheckoutMetadata(command.Metadata), expiry, now);
        var intent = new PaymentIntent(Guid.NewGuid(), session.CheckoutSessionId, session.MerchantId, session.AmountMinor, session.Currency, command.PaymentIntentIdempotencyKey, expiry, now);
        session.AttachPaymentIntent(intent.PaymentIntentId, now);
        await sessions.AddAsync(session, ct); await intents.AddAsync(intent, ct); await Audit(session, intent, "checkout.created", command.Actor, ct); return Map(session);
    }
    public async Task<CheckoutSessionResult> AttachPaymentMethodAsync(AttachCheckoutPaymentMethodCommand command, CancellationToken ct = default)
    { var session = await RequiredSession(command.CheckoutSessionId, ct); var intent = await RequiredIntent(session.PaymentIntentId ?? Guid.Empty, ct); intent.AttachPaymentMethod(new PaymentMethodReference(command.PaymentMethodType, command.TokenReference), clock.UtcNow); session.MarkReadyForPayment(clock.UtcNow); await intents.SaveAsync(intent, ct); await sessions.SaveAsync(session, ct); await Audit(session, intent, "checkout.payment_method_attached", command.Actor, ct); return Map(session); }
    public async Task<CheckoutSessionResult> CancelAsync(CancelCheckoutSessionCommand command, CancellationToken ct = default)
    { var session = await RequiredSession(command.CheckoutSessionId, ct); var intent = await RequiredIntent(session.PaymentIntentId ?? Guid.Empty, ct); session.Cancel(clock.UtcNow); intent.Cancel(clock.UtcNow); await sessions.SaveAsync(session, ct); await intents.SaveAsync(intent, ct); await Audit(session, intent, "checkout.cancelled", command.Actor, ct); return Map(session); }
    public async Task<CheckoutSessionResult> ExpireAsync(ExpireCheckoutSessionCommand command, CancellationToken ct = default)
    { var session = await RequiredSession(command.CheckoutSessionId, ct); var intent = await RequiredIntent(session.PaymentIntentId ?? Guid.Empty, ct); session.Expire(clock.UtcNow); intent.Expire(clock.UtcNow); await sessions.SaveAsync(session, ct); await intents.SaveAsync(intent, ct); await Audit(session, intent, "checkout.expired", command.Actor, ct); return Map(session); }
    public async Task<CheckoutSessionResult> GetSessionAsync(Guid id, CancellationToken ct = default) => Map(await RequiredSession(id, ct));
    public async Task<PaymentIntentResult> GetPaymentIntentAsync(Guid id, CancellationToken ct = default) => Map(await RequiredIntent(id, ct));
    private async Task<CheckoutSession> RequiredSession(Guid id, CancellationToken ct) => await sessions.GetAsync(id, ct) ?? throw new KeyNotFoundException("Checkout session not found.");
    private async Task<PaymentIntent> RequiredIntent(Guid id, CancellationToken ct) => await intents.GetAsync(id, ct) ?? throw new KeyNotFoundException("Payment intent not found.");
    private async Task Audit(CheckoutSession session, PaymentIntent intent, string eventType, string actor, CancellationToken ct) => await audit.AppendAsync(new CheckoutAuditEvent(Guid.NewGuid(), session.CheckoutSessionId, intent.PaymentIntentId, session.MerchantId, eventType, actor, clock.UtcNow, new Dictionary<string,string> { ["checkoutStatus"] = session.Status.ToString(), ["paymentIntentStatus"] = intent.Status.ToString(), ["amountMinor"] = intent.AmountMinor.ToString(), ["currency"] = intent.Currency, ["authorizationPerformed"] = "false", ["capturePerformed"] = "false", ["settlementPerformed"] = "false", ["payoutPerformed"] = "false", ["moneyMovementPerformed"] = "false", ["ledgerMutationPerformed"] = "false" }), ct);
    private static CheckoutSessionResult Map(CheckoutSession s) => new(s.CheckoutSessionId, s.MerchantId, s.CustomerReference, s.MerchantOrderReference, s.AmountMinor, s.Currency, s.ReturnUrl, s.Status, s.PaymentIntentId ?? throw new InvalidOperationException("Checkout has no payment intent."), s.ExpiresAtUtc, s.CreatedAtUtc, s.UpdatedAtUtc);
    private static PaymentIntentResult Map(PaymentIntent i) => new(i.PaymentIntentId, i.CheckoutSessionId, i.MerchantId, i.AmountMinor, i.Currency, i.Status, i.PaymentMethod?.Type, i.ExpiresAtUtc, i.CreatedAtUtc, i.UpdatedAtUtc);
}
