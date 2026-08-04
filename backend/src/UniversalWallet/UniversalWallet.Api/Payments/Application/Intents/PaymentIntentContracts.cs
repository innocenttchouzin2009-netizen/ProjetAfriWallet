using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Intents;

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> GetAsync(Guid intentId, CancellationToken cancellationToken);
    Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid payerAwid, string idempotencyKey, CancellationToken cancellationToken);
    Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentIntent>> ListAsync(Guid payerAwid, PaymentIntentStatus? status, CancellationToken cancellationToken);
}

public interface IPaymentRecipientResolver
{
    Task<ResolvedRecipient?> ResolveAsync(RecipientType type, string reference, CancellationToken cancellationToken);
}

public sealed record ResolvedRecipient(Guid? TargetWalletId, string DisplayReference);

public interface IPaymentWalletReader
{
    Task<PaymentWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken);
}

public sealed record PaymentWalletSnapshot(Guid Id, Guid AwidId, string Currency, WalletStatus Status);
