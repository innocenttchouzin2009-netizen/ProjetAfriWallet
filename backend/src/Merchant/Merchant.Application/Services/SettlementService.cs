using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class SettlementService
{
    private readonly List<MerchantSettlement> _settlements = [];
    private readonly Dictionary<string, SettlementInstruction> _instructions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SettlementBatch> _batches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SettlementTransaction> _transactions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _auditEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _telemetryEvents = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<MerchantSettlement>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MerchantSettlement>>(_settlements);

    public Task<MerchantSettlement> CreateAsync(MerchantSettlement settlement, CancellationToken cancellationToken = default)
    {
        settlement.SettlementId = string.IsNullOrWhiteSpace(settlement.SettlementId) ? Guid.NewGuid().ToString("N") : settlement.SettlementId;
        settlement.CreatedAt = settlement.CreatedAt == default ? DateTimeOffset.UtcNow : settlement.CreatedAt;
        _settlements.Add(settlement);
        return Task.FromResult(settlement);
    }

    public SettlementInstruction CreateInstruction(SettlementInstruction instruction)
    {
        instruction.SettlementId = string.IsNullOrWhiteSpace(instruction.SettlementId) ? $"sett-{Guid.NewGuid():N}" : instruction.SettlementId;
        instruction.NetAmountMinor = instruction.GrossAmountMinor - instruction.FeeAmountMinor - instruction.TaxAmountMinor;
        instruction.Status = SettlementStatus.CREATED;
        instruction.CorrelationId ??= $"corr-{Guid.NewGuid():N}";
        instruction.TraceId ??= $"trace-{Guid.NewGuid():N}";
        _instructions[instruction.SettlementId] = instruction;
        AddAudit(instruction.MerchantId, "SETTLEMENT_CREATED");
        AddTelemetry(instruction.MerchantId, "merchant.settlement.created");
        return instruction;
    }

    public IReadOnlyList<SettlementInstruction> ListInstructions() => _instructions.Values.ToList();

    public SettlementInstruction? GetInstruction(string settlementId)
        => _instructions.TryGetValue(settlementId, out var instruction) ? instruction : null;

    public SettlementBatch CreateBatch(string merchantId, string batchCode, IReadOnlyList<string> settlementIds)
    {
        var batch = new SettlementBatch
        {
            BatchId = $"batch-{Guid.NewGuid():N}",
            MerchantId = merchantId,
            BatchCode = batchCode,
            SettlementIds = settlementIds.ToList(),
            Status = SettlementStatus.SCHEDULED
        };

        _batches[batch.BatchId] = batch;
        AddAudit(merchantId, "SETTLEMENT_SCHEDULED");
        AddTelemetry(merchantId, "merchant.settlement.scheduled");
        return batch;
    }

    public decimal CalculateFee(decimal grossAmountMinor, SettlementMethod method)
        => method switch
        {
            SettlementMethod.AFRIWALLET_WALLET => grossAmountMinor * 0.025m,
            SettlementMethod.BANK_TRANSFER => grossAmountMinor * 0.02m,
            SettlementMethod.MTN_MOMO => grossAmountMinor * 0.03m,
            SettlementMethod.ORANGE_MONEY => grossAmountMinor * 0.025m,
            _ => 0m
        };

    public decimal CalculateTax(decimal grossAmountMinor, SettlementMethod method)
        => method switch
        {
            SettlementMethod.AFRIWALLET_WALLET => grossAmountMinor * 0.01m,
            SettlementMethod.BANK_TRANSFER => grossAmountMinor * 0.01m,
            SettlementMethod.MTN_MOMO => grossAmountMinor * 0.01m,
            SettlementMethod.ORANGE_MONEY => grossAmountMinor * 0.01m,
            _ => 0m
        };

    public SettlementInstruction ExecuteInstruction(string settlementId, SettlementMethod method)
    {
        var instruction = GetInstruction(settlementId) ?? throw new InvalidOperationException("Settlement was not found.");
        instruction.Status = SettlementStatus.PROCESSING;
        instruction.SettlementMethod = method;
        instruction.ExecutedAt = DateTimeOffset.UtcNow;
        instruction.Status = SettlementStatus.COMPLETED;
        var transaction = new SettlementTransaction
        {
            TransactionId = $"settx-{Guid.NewGuid():N}",
            SettlementId = instruction.SettlementId,
            MerchantId = instruction.MerchantId,
            PaymentReference = instruction.PaymentReference,
            NetAmountMinor = instruction.NetAmountMinor,
            CurrencyCode = instruction.CurrencyCode,
            SettlementMethod = method,
            Status = SettlementStatus.COMPLETED
        };
        _transactions[transaction.TransactionId] = transaction;
        AddAudit(instruction.MerchantId, "SETTLEMENT_COMPLETED");
        AddTelemetry(instruction.MerchantId, "merchant.settlement.completed");
        return instruction;
    }

    public SettlementInstruction FailInstruction(string settlementId, string reason)
    {
        var instruction = GetInstruction(settlementId) ?? throw new InvalidOperationException("Settlement was not found.");
        instruction.Status = SettlementStatus.FAILED;
        AddAudit(instruction.MerchantId, "SETTLEMENT_FAILED");
        AddTelemetry(instruction.MerchantId, "merchant.settlement.failed");
        return instruction;
    }

    public SettlementInstruction RecoverInstruction(string settlementId)
    {
        var instruction = GetInstruction(settlementId) ?? throw new InvalidOperationException("Settlement was not found.");
        instruction.Status = SettlementStatus.COMPLETED;
        instruction.ExecutedAt = DateTimeOffset.UtcNow;
        AddAudit(instruction.MerchantId, "SETTLEMENT_REVERSED");
        AddTelemetry(instruction.MerchantId, "merchant.settlement.recovered");
        return instruction;
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
