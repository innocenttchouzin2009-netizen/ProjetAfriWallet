using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestWalletOwnershipReader
{
    Task<bool> IsOwnedByAsync(
        WalletId walletId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestPaymentPort
{
    Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default);
}
