using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.QrPayments;

public interface IQrPaymentGateway
{
    Task<QrPayment> GenerateAsync(
        GenerateQrCommand command,
        CancellationToken cancellationToken = default);

    Task<DecodedQrPayload> DecodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<QrPayment?> FindByQrIdAsync(
        string qrId,
        CancellationToken cancellationToken = default);

    Task<QrPayment?> FindByTransferIntentIdAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default);

    Task<QrPayment> InitiateAsync(
        InitiateQrPaymentCommand command,
        CancellationToken cancellationToken = default);

    Task<QrReceiptPayload> GenerateReceiptAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTimelineAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default);
}
