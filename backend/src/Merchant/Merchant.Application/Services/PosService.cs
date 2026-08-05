using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class PosService
{
    private readonly Dictionary<string, PosTerminal> _terminals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PosTransaction> _transactions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PosReceipt> _receipts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _auditEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _telemetryEvents = new(StringComparer.OrdinalIgnoreCase);

    public PosTerminal RegisterTerminal(PosTerminal terminal)
    {
        terminal.TerminalId = string.IsNullOrWhiteSpace(terminal.TerminalId) ? $"term-{Guid.NewGuid():N}" : terminal.TerminalId;
        terminal.CreatedAt = terminal.CreatedAt == default ? DateTimeOffset.UtcNow : terminal.CreatedAt;
        terminal.UpdatedAt = DateTimeOffset.UtcNow;
        _terminals[terminal.TerminalId] = terminal;
        AddAudit(terminal.MerchantId, "POS_TERMINAL_REGISTERED");
        AddTelemetry(terminal.MerchantId, "merchant.pos.terminal.registered");
        return terminal;
    }

    public PosTerminal Heartbeat(string terminalId)
    {
        var terminal = GetTerminal(terminalId);
        terminal.Status = PosTerminalStatus.Active;
        terminal.LastHeartbeatUtc = DateTimeOffset.UtcNow;
        terminal.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(terminal.MerchantId, "POS_TERMINAL_HEARTBEAT");
        AddTelemetry(terminal.MerchantId, "merchant.pos.terminal.heartbeat");
        return terminal;
    }

    public PosTransaction CreateCheckout(PosCheckoutRequest request)
    {
        var terminal = _terminals.Values.SingleOrDefault(item => string.Equals(item.TerminalCode, request.TerminalCode, StringComparison.OrdinalIgnoreCase));
        if (terminal is null)
        {
            throw new InvalidOperationException("Terminal was not found.");
        }

        var transaction = new PosTransaction
        {
            TransactionId = $"txn-{Guid.NewGuid():N}",
            MerchantId = request.MerchantId,
            TerminalId = terminal.TerminalId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode,
            Channel = PosChannel.WebCheckout,
            Description = request.Description,
            Status = PosTransactionStatus.Initiated
        };

        _transactions[transaction.TransactionId] = transaction;
        AddAudit(request.MerchantId, "POS_CHECKOUT_CREATED");
        AddTelemetry(request.MerchantId, "merchant.pos.checkout.created");
        return transaction;
    }

    public PosTransaction InitiatePayment(PosPaymentRequest request)
    {
        var terminal = GetTerminal(request.TerminalId);
        var transaction = new PosTransaction
        {
            TransactionId = $"txn-{Guid.NewGuid():N}",
            MerchantId = request.MerchantId,
            TerminalId = terminal.TerminalId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode,
            Channel = request.Channel,
            Description = request.Description,
            Status = PosTransactionStatus.Initiated,
            TransferIntentId = $"transfer-{Guid.NewGuid():N}"
        };

        _transactions[transaction.TransactionId] = transaction;
        AddAudit(request.MerchantId, "POS_PAYMENT_INITIATED");
        AddTelemetry(request.MerchantId, "merchant.pos.payment.initiated");
        return transaction;
    }

    public PosTransaction CompletePayment(string transactionId)
    {
        var transaction = GetTransaction(transactionId);
        if (transaction is null)
        {
            throw new InvalidOperationException("Transaction was not found.");
        }

        transaction.Status = PosTransactionStatus.Completed;
        transaction.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(transaction.MerchantId, "POS_PAYMENT_COMPLETED");
        AddTelemetry(transaction.MerchantId, "merchant.pos.payment.completed");
        return transaction;
    }

    public PosReceipt GenerateReceipt(string transactionId)
    {
        var transaction = GetTransaction(transactionId);
        if (transaction is null)
        {
            throw new InvalidOperationException("Transaction was not found.");
        }

        var receipt = new PosReceipt
        {
            ReceiptId = $"receipt-{Guid.NewGuid():N}",
            TransactionId = transaction.TransactionId,
            MerchantId = transaction.MerchantId,
            TerminalId = transaction.TerminalId,
            Code = $"AFW-{transaction.TransactionId[^8..].ToUpperInvariant()}",
            Status = "Issued"
        };

        _receipts[receipt.ReceiptId] = receipt;
        transaction.ReceiptId = receipt.ReceiptId;
        AddAudit(transaction.MerchantId, "POS_RECEIPT_GENERATED");
        AddTelemetry(transaction.MerchantId, "merchant.pos.receipt.generated");
        return receipt;
    }

    public PosTransaction? GetTransaction(string transactionId)
        => _transactions.TryGetValue(transactionId, out var transaction) ? transaction : null;

    public IReadOnlyList<PosTransaction> GetTransactions()
        => _transactions.Values.OrderBy(item => item.CreatedAt).ToList();

    public PosReceipt? GetReceipt(string receiptId)
        => _receipts.TryGetValue(receiptId, out var receipt) ? receipt : null;

    public PosTerminal GetTerminal(string terminalId)
    {
        if (_terminals.TryGetValue(terminalId, out var terminal))
        {
            return terminal;
        }

        throw new InvalidOperationException("Terminal was not found.");
    }

    public IReadOnlyList<string> GetAuditEvents(string merchantId)
        => _auditEvents.TryGetValue(merchantId, out var events) ? events : [];

    public IReadOnlyList<string> GetTelemetryEvents(string merchantId)
        => _telemetryEvents.TryGetValue(merchantId, out var events) ? events : [];

    private void AddAudit(string merchantId, string evt)
    {
        if (!_auditEvents.TryGetValue(merchantId, out var events))
        {
            events = [];
            _auditEvents[merchantId] = events;
        }

        events.Add(evt);
    }

    private void AddTelemetry(string merchantId, string evt)
    {
        if (!_telemetryEvents.TryGetValue(merchantId, out var events))
        {
            events = [];
            _telemetryEvents[merchantId] = events;
        }

        events.Add(evt);
    }
}
