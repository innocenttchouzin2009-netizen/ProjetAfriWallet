using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class QrPaymentService
{
    private readonly List<QrPayment> _payments = [];

    public Task<IReadOnlyList<QrPayment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<QrPayment>>(_payments);

    public Task<QrPayment> CreateAsync(QrPayment payment, CancellationToken cancellationToken = default)
    {
        payment.PaymentId = string.IsNullOrWhiteSpace(payment.PaymentId) ? Guid.NewGuid().ToString("N") : payment.PaymentId;
        payment.CreatedAt = payment.CreatedAt == default ? DateTimeOffset.UtcNow : payment.CreatedAt;
        _payments.Add(payment);
        return Task.FromResult(payment);
    }
}
