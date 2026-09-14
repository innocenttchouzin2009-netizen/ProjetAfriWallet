namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestMailboxQueryPort
{
    Task<PaymentRequestListPage> ListAsync(
        PaymentRequestListQuery query,
        CancellationToken cancellationToken = default);
}
