namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestPaymentReceiptReader
{
    Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
