using UniversalWallet.Api.Payments.Domain.Receipts;

namespace UniversalWallet.Api.Payments.Application.Receipts;

public interface IPaymentReceiptRepository
{
    Task<PaymentReceipt?> GetAsync(Guid receiptId, CancellationToken cancellationToken = default);
    Task<PaymentReceipt?> GetByIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default);
    Task<PaymentReceipt?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}

public sealed record GenerateReceiptRequest(Guid PaymentIntentId);
public sealed record GenerateReceiptResponse(PaymentReceipt Receipt);
public sealed record VerifyReceiptResponse(bool Valid, string? Status, long? AmountMinor, string? CurrencyCode, string? PublicReference, DateTimeOffset? PaidAt, int? DocumentVersion);
