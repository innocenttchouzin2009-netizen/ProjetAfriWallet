using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.DeliverySubscriptions.Persistence;

public sealed class EfPaymentRequestDeliveryRecipientResolver(PaymentRequestDbContext db)
    : IPaymentRequestDeliveryRecipientResolver
{
    public async Task<RecipientReference?> ResolveAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        if (paymentRequestId == Guid.Empty)
            throw new ArgumentException("Payment request id cannot be empty.", nameof(paymentRequestId));

        var row = await db.PaymentRequests.AsNoTracking()
            .Where(x => x.Id == paymentRequestId)
            .Select(x => new { x.PayerReferenceKind, x.PayerReferenceValue })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        return (RecipientReferenceKind)row.PayerReferenceKind switch
        {
            RecipientReferenceKind.AfWalId => RecipientReference.FromAfWalId(row.PayerReferenceValue),
            RecipientReferenceKind.QrToken => RecipientReference.FromQrToken(row.PayerReferenceValue),
            _ => throw new InvalidOperationException("Payment request contains an unsupported recipient reference kind.")
        };
    }
}
