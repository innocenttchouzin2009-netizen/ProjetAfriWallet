using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Receipts;
using UniversalWallet.Api.Payments.Domain.Settlements;
using UniversalWallet.Api.Payments.Domain.Transfers;

namespace UniversalWallet.Api.Payments.Application.Receipts;

public sealed class GenerateReceiptHandler
{
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentTransferRepository _transferRepository;
    private readonly ISettlementRepository _settlementRepository;
    private readonly IPaymentReceiptRepository _receiptRepository;

    public GenerateReceiptHandler(
        IPaymentIntentRepository intentRepository,
        IPaymentTransferRepository transferRepository,
        ISettlementRepository settlementRepository,
        IPaymentReceiptRepository receiptRepository)
    {
        _intentRepository = intentRepository;
        _transferRepository = transferRepository;
        _settlementRepository = settlementRepository;
        _receiptRepository = receiptRepository;
    }

    public async Task<GenerateReceiptResponse> HandleAsync(GenerateReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _receiptRepository.GetByIntentAsync(request.PaymentIntentId, cancellationToken);
        if (existing is not null)
        {
            return new GenerateReceiptResponse(existing);
        }

        var intent = await _intentRepository.GetAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        var transfer = await _transferRepository.GetByIntentAsync(request.PaymentIntentId, cancellationToken);
        if (transfer is null || transfer.Status != PaymentTransferStatus.Completed)
        {
            throw new InvalidOperationException("PAYMENT_RECEIPT_NOT_AVAILABLE");
        }

        var settlement = await _settlementRepository.GetByTransferIdAsync(transfer.TransferId, cancellationToken);
        if (settlement is null || settlement.Status != SettlementStatus.SETTLED)
        {
            throw new InvalidOperationException("PAYMENT_RECEIPT_NOT_AVAILABLE");
        }

        var receipt = new PaymentReceipt
        {
            PaymentIntentId = intent.Id,
            TransferId = transfer.TransferId,
            SettlementId = settlement.SettlementId,
            PublicReference = $"AFW-PAY-{intent.Id.ToString("N")[..8].ToUpperInvariant()}",
            ReceiptNumber = $"AFW-RCP-{DateTimeOffset.UtcNow:yyyy-000000}",
            SenderDisplay = "Innocent T.",
            RecipientDisplay = "Marie K.",
            AmountMinor = intent.AmountMinor,
            CurrencyCode = intent.CurrencyCode,
            FeeMinor = 0,
            Purpose = intent.Purpose.ToString(),
            PaidAt = transfer.ExecutedAt,
            SettledAt = settlement.SettledAt,
            VerificationTokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(intent.Id.ToString("D")))),
            Signature = "server-signature"
        };

        await _receiptRepository.AddAsync(receipt, cancellationToken);
        return new GenerateReceiptResponse(receipt);
    }
}
