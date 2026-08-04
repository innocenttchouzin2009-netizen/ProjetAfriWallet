using UniversalWallet.Api.Payments.Application.Receipts;
using UniversalWallet.Api.Payments.Domain.Receipts;

namespace UniversalWallet.Api.Payments.Infrastructure.Receipts;

public sealed class InMemoryReceiptRepository : IPaymentReceiptRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentReceipt> _receipts = new();
    private readonly Dictionary<Guid, Guid> _byIntent = new();
    private readonly Dictionary<string, Guid> _byTokenHash = new(StringComparer.OrdinalIgnoreCase);

    public Task<PaymentReceipt?> GetAsync(Guid receiptId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_receipts.TryGetValue(receiptId, out var receipt) ? receipt : null);
        }
    }

    public Task<PaymentReceipt?> GetByIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byIntent.TryGetValue(paymentIntentId, out var receiptId) && _receipts.TryGetValue(receiptId, out var receipt) ? receipt : null);
        }
    }

    public Task AddAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _receipts[receipt.ReceiptId] = receipt;
            _byIntent[receipt.PaymentIntentId] = receipt.ReceiptId;
            if (!string.IsNullOrWhiteSpace(receipt.VerificationTokenHash))
            {
                _byTokenHash[receipt.VerificationTokenHash] = receipt.ReceiptId;
            }

            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _receipts[receipt.ReceiptId] = receipt;
            _byIntent[receipt.PaymentIntentId] = receipt.ReceiptId;
            if (!string.IsNullOrWhiteSpace(receipt.VerificationTokenHash))
            {
                _byTokenHash[receipt.VerificationTokenHash] = receipt.ReceiptId;
            }

            return Task.CompletedTask;
        }
    }

    public Task<PaymentReceipt?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byTokenHash.TryGetValue(tokenHash, out var receiptId) && _receipts.TryGetValue(receiptId, out var receipt) ? receipt : null);
        }
    }
}
