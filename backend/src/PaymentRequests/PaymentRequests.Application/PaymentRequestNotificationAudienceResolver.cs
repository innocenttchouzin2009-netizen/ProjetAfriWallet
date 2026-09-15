using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestNotificationAudienceResolver(
    IWalletRepository walletRepository,
    IPaymentRequestRepository paymentRequestRepository,
    IPaymentRequestRecipientResolver recipientResolver)
    : IPaymentRequestNotificationAudienceResolver
{
    public async Task<IReadOnlyList<PaymentRequestNotificationRecipient>> ResolveAsync(
        PaymentRequestNotificationProjectionSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        var requesterWallet = await walletRepository.GetAsync(source.RequesterWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Requester wallet referenced by lifecycle event was not found.");

        WalletId? payerWalletId = source.AcceptedPayerWalletId;
        if (payerWalletId is null)
        {
            var request = await paymentRequestRepository.GetAsync(source.PaymentRequestId, cancellationToken)
                ?? throw new InvalidOperationException("Payment request referenced by lifecycle event was not found.");

            payerWalletId = await recipientResolver.ResolveAsync(
                request.PayerReference,
                source.Currency,
                cancellationToken);

            if (payerWalletId is null)
            {
                throw new InvalidOperationException("Payer referenced by payment request could not be resolved.");
            }
        }

        var payerWallet = await walletRepository.GetAsync(payerWalletId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Payer wallet referenced by lifecycle event was not found.");

        return
        [
            new PaymentRequestNotificationRecipient(
                requesterWallet.OwnerId,
                PaymentRequestNotificationAudience.Requester),
            new PaymentRequestNotificationRecipient(
                payerWallet.OwnerId,
                PaymentRequestNotificationAudience.Payer)
        ];
    }
}
