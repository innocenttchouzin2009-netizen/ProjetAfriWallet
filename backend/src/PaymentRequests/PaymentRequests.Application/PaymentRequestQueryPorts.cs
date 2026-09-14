namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestQueryRepository
{
    Task<PaymentRequestQueryPage> ListReceivedAsync(
        ReceivedPaymentRequestsQuery query,
        CancellationToken cancellationToken = default);

    Task<PaymentRequestQueryPage> ListSentAsync(
        SentPaymentRequestsQuery query,
        CancellationToken cancellationToken = default);
}
