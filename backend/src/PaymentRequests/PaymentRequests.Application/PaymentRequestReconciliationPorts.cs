namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestPaymentReceiptReader
{
    Task<PaymentRequestReconciliationReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
