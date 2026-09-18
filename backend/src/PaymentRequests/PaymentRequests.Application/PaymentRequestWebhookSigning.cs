namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestWebhookSigner
{
    string Sign(string payloadJson);
}
