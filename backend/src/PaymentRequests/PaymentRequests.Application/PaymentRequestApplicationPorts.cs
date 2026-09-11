using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestRepository
{
    Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default);
    Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IPaymentRequestRecipientResolver
{
    Task<WalletId?> ResolveAsync(
        RecipientReference reference,
        Currency currency,
        CancellationToken cancellationToken = default);
}
