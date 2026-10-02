using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.QrPayments;

public sealed class QrPaymentGateway(QrPaymentService service) : IQrPaymentGateway
{
    public Task<QrPayment> GenerateAsync(
        GenerateQrCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(service.GenerateQr(command));
    }

    public Task<DecodedQrPayload> DecodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(service.DecodeQr(code));
    }

    public async Task<QrPayment?> FindByQrIdAsync(
        string qrId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payments = await service.GetAllAsync(cancellationToken);
        return payments.SingleOrDefault(
            item => string.Equals(item.QrId, qrId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<QrPayment?> FindByTransferIntentIdAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payments = await service.GetAllAsync(cancellationToken);
        return payments.SingleOrDefault(
            item => string.Equals(
                item.TransferIntentId,
                transferIntentId,
                StringComparison.OrdinalIgnoreCase));
    }

    public Task<QrPayment> InitiateAsync(
        InitiateQrPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payment = service.InitiatePayment(command);

        if (payment.Type == QrPaymentType.Dynamic)
        {
            payment.AmountMinor = command.Amount;
            payment.Currency = command.Currency.ToUpperInvariant();
            payment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return Task.FromResult(payment);
    }

    public Task<QrReceiptPayload> GenerateReceiptAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(service.GenerateReceipt(transferIntentId));
    }

    public Task<IReadOnlyList<string>> GetTimelineAsync(
        string transferIntentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(service.GetTimeline(transferIntentId));
    }
}
