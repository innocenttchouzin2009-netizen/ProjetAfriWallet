using AfriWallet.Merchants.Receivables.Domain;

namespace AfriWallet.Merchants.Receivables.Application;

public sealed record ConfigureMerchantFeeScheduleCommand(
    string MerchantId,
    string Currency,
    int PercentageBasisPoints,
    long FixedFeeMinor,
    string Actor);

public sealed record CreateMerchantReceivableCommand(
    Guid CaptureExecutionId,
    string IdempotencyKey,
    string Actor);

public sealed record ApplyMerchantSettlementReceiptCommand(
    Guid ReceivableId,
    Guid SettlementId,
    long SettledAmountMinor,
    string Currency,
    string Actor);

public sealed record MerchantReceivableResult(
    Guid ReceivableId,
    Guid CaptureExecutionId,
    Guid DecisionId,
    Guid PaymentIntentId,
    string MerchantId,
    string Currency,
    long GrossAmountMinor,
    long FeeAmountMinor,
    long NetAmountMinor,
    MerchantReceivableStatus Status,
    Guid? SettlementId,
    string IdempotencyKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SettledAtUtc);

public sealed class MerchantReceivableService(
    IMerchantCaptureReceivableReader captures,
    IMerchantRegistryReceivableReader merchants,
    IMerchantFeeScheduleStore feeSchedules,
    IMerchantReceivableRepository receivables,
    IMerchantReceivableAuditStore audit,
    TimeProvider timeProvider)
{
    public async Task<MerchantFeeSchedule> ConfigureFeeScheduleAsync(
        ConfigureMerchantFeeScheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var merchant = await merchants.GetAsync(command.MerchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant was not found.");

        var schedule = new MerchantFeeSchedule(
            merchant.MerchantId,
            command.Currency,
            command.PercentageBasisPoints,
            command.FixedFeeMinor);

        if (!string.Equals(schedule.Currency, merchant.SettlementCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fee schedule currency must match the merchant settlement currency.");

        await feeSchedules.UpsertAsync(schedule, cancellationToken);
        return schedule;
    }

    public async Task<MerchantReceivableResult> CreateFromCaptureAsync(
        CreateMerchantReceivableCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CaptureExecutionId == Guid.Empty)
            throw new ArgumentException("Capture execution id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Idempotency key and actor are required.", nameof(command));

        var byKey = await receivables.GetByIdempotencyKeyAsync(command.IdempotencyKey.Trim(), cancellationToken);
        if (byKey is not null)
        {
            if (byKey.CaptureExecutionId != command.CaptureExecutionId)
                throw new InvalidOperationException("Idempotency key belongs to a different capture.");
            return Map(byKey);
        }

        var byCapture = await receivables.GetByCaptureAsync(command.CaptureExecutionId, cancellationToken);
        if (byCapture is not null)
            return Map(byCapture);

        var capture = await captures.GetAsync(command.CaptureExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant capture was not found.");

        if (!string.Equals(capture.Status, "Captured", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only completed merchant captures can create receivables.");

        var merchant = await merchants.GetAsync(capture.MerchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant was not found.");
        if (!string.Equals(merchant.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Merchant must be active before a receivable can be created.");
        if (!string.Equals(merchant.SettlementCurrency, capture.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Capture currency must match the merchant settlement currency.");

        var schedule = await feeSchedules.GetAsync(merchant.MerchantId, capture.Currency, cancellationToken)
            ?? throw new InvalidOperationException("Merchant fee schedule is not configured.");

        var value = MerchantReceivable.Create(
            capture.CaptureExecutionId,
            capture.DecisionId,
            capture.PaymentIntentId,
            capture.MerchantId,
            capture.AmountMinor,
            capture.Currency,
            schedule,
            command.IdempotencyKey,
            timeProvider.GetUtcNow());

        await receivables.AddAsync(value, cancellationToken);
        await WriteAuditAsync(value, "receivable.created", command.Actor, cancellationToken);
        return Map(value);
    }

    public async Task<MerchantReceivableResult> ApplySettlementReceiptAsync(
        ApplyMerchantSettlementReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var value = await receivables.GetAsync(command.ReceivableId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant receivable was not found.");

        value.ApplySettlementReceipt(
            command.SettlementId,
            command.SettledAmountMinor,
            command.Currency,
            timeProvider.GetUtcNow());

        await receivables.SaveAsync(value, cancellationToken);
        await WriteAuditAsync(value, "receivable.settled", command.Actor, cancellationToken);
        return Map(value);
    }

    public async Task<IReadOnlyCollection<MerchantReceivableResult>> ListOpenAsync(
        string merchantId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var values = await receivables.ListOpenAsync(merchantId, currency, cancellationToken);
        return values.Select(Map).ToArray();
    }

    private Task WriteAuditAsync(MerchantReceivable value, string eventType, string actor, CancellationToken ct) =>
        audit.AppendAsync(
            new MerchantReceivableAuditEvent(
                Guid.NewGuid(),
                value.ReceivableId,
                value.CaptureExecutionId,
                value.MerchantId,
                eventType,
                actor,
                timeProvider.GetUtcNow(),
                new Dictionary<string,string>
                {
                    ["status"] = value.Status.ToString(),
                    ["currency"] = value.Currency,
                    ["grossAmountMinor"] = value.GrossAmountMinor.ToString(),
                    ["feeAmountMinor"] = value.FeeAmountMinor.ToString(),
                    ["netAmountMinor"] = value.NetAmountMinor.ToString(),
                    ["moneyMovementPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false"
                }),
            ct);

    private static MerchantReceivableResult Map(MerchantReceivable value) =>
        new(
            value.ReceivableId,
            value.CaptureExecutionId,
            value.DecisionId,
            value.PaymentIntentId,
            value.MerchantId,
            value.Currency,
            value.GrossAmountMinor,
            value.FeeAmountMinor,
            value.NetAmountMinor,
            value.Status,
            value.SettlementId,
            value.IdempotencyKey,
            value.CreatedAtUtc,
            value.UpdatedAtUtc,
            value.SettledAtUtc);
}
