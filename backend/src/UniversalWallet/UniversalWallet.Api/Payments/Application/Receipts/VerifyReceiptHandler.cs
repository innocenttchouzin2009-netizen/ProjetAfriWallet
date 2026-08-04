using UniversalWallet.Api.Payments.Domain.Receipts;

namespace UniversalWallet.Api.Payments.Application.Receipts;

public sealed class VerifyReceiptHandler
{
    private readonly IPaymentReceiptRepository _receiptRepository;

    public VerifyReceiptHandler(IPaymentReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
    }

    public async Task<VerifyReceiptResponse> HandleAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        var receipt = await _receiptRepository.GetByVerificationTokenHashAsync(tokenHash, cancellationToken);
        if (receipt is null)
        {
            return new VerifyReceiptResponse(false, null, null, null, null, null, null);
        }

        if (receipt.Status == PaymentReceiptStatus.Revoked)
        {
            return new VerifyReceiptResponse(false, "REVOKED", receipt.AmountMinor, receipt.CurrencyCode, receipt.PublicReference, receipt.PaidAt, receipt.DocumentVersion);
        }

        return new VerifyReceiptResponse(true, receipt.Status.ToString().ToUpperInvariant(), receipt.AmountMinor, receipt.CurrencyCode, receipt.PublicReference, receipt.PaidAt, receipt.DocumentVersion);
    }
}
