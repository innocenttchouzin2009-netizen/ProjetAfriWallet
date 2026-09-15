namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestReconciliationPort
{
    Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
