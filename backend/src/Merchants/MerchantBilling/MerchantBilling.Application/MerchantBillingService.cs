using AfriWallet.Merchants.Billing.Domain;

namespace AfriWallet.Merchants.Billing.Application;

public sealed record ProcessMerchantBillingCaptureCommand(Guid CaptureExecutionId, string Actor);

public sealed record MerchantBillingResult(
    Guid HandoffId,
    Guid CaptureExecutionId,
    string MerchantId,
    MerchantBillingHandoffStatus Status,
    Guid? ReceivableId,
    int AttemptCount,
    string? LastError,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class MerchantBillingService(
    IMerchantBillingCaptureReader captures,
    IMerchantBillingReceivablePort receivables,
    IMerchantBillingHandoffRepository handoffs,
    IMerchantBillingAuditStore audit,
    TimeProvider timeProvider)
{
    public async Task<MerchantBillingResult> ProcessCaptureAsync(
        ProcessMerchantBillingCaptureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CaptureExecutionId == Guid.Empty)
            throw new ArgumentException("Capture execution id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var existing = await handoffs.GetByCaptureAsync(command.CaptureExecutionId, cancellationToken);
        if (existing?.Status == MerchantBillingHandoffStatus.Completed)
            return Map(existing);

        var capture = await captures.GetAsync(command.CaptureExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant capture was not found.");

        if (!string.Equals(capture.Status, "Captured", StringComparison.OrdinalIgnoreCase) ||
            !capture.SettlementReady)
            throw new InvalidOperationException("Merchant capture is not billing eligible.");

        var handoff = existing ?? MerchantBillingHandoff.Create(
            capture.CaptureExecutionId,
            capture.MerchantId,
            timeProvider.GetUtcNow());

        if (existing is null)
        {
            await handoffs.AddAsync(handoff, cancellationToken);
            await WriteAudit(handoff, "billing.handoff.created", command.Actor, cancellationToken);
        }

        handoff.BeginAttempt(timeProvider.GetUtcNow());
        await handoffs.SaveAsync(handoff, cancellationToken);

        try
        {
            var receivable = await receivables.CreateFromCaptureAsync(
                capture.CaptureExecutionId,
                $"merchant-billing:{capture.CaptureExecutionId:N}",
                command.Actor,
                cancellationToken);

            if (receivable.CaptureExecutionId != capture.CaptureExecutionId ||
                !string.Equals(receivable.MerchantId, capture.MerchantId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Receivable handoff result does not match the capture.");

            handoff.Complete(receivable.ReceivableId, timeProvider.GetUtcNow());
            await handoffs.SaveAsync(handoff, cancellationToken);
            await WriteAudit(handoff, "billing.handoff.completed", command.Actor, cancellationToken);
            return Map(handoff);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            handoff.RecordFailure(exception.Message, timeProvider.GetUtcNow());
            await handoffs.SaveAsync(handoff, CancellationToken.None);
            await WriteAudit(handoff, "billing.handoff.failed", command.Actor, CancellationToken.None);
            throw;
        }
    }

    public async Task<MerchantBillingResult> GetAsync(
        Guid handoffId,
        CancellationToken cancellationToken = default) =>
        Map(await handoffs.GetAsync(handoffId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant billing handoff was not found."));

    private Task WriteAudit(
        MerchantBillingHandoff handoff,
        string eventType,
        string actor,
        CancellationToken cancellationToken) =>
        audit.AppendAsync(
            new MerchantBillingAuditEvent(
                Guid.NewGuid(),
                handoff.HandoffId,
                handoff.CaptureExecutionId,
                handoff.MerchantId,
                eventType,
                actor,
                timeProvider.GetUtcNow(),
                new Dictionary<string,string>
                {
                    ["status"] = handoff.Status.ToString(),
                    ["attemptCount"] = handoff.AttemptCount.ToString(),
                    ["receivableCreated"] = (handoff.ReceivableId is not null).ToString().ToLowerInvariant(),
                    ["settlementPerformed"] = "false",
                    ["payoutPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false"
                }),
            cancellationToken);

    private static MerchantBillingResult Map(MerchantBillingHandoff value) =>
        new(
            value.HandoffId,
            value.CaptureExecutionId,
            value.MerchantId,
            value.Status,
            value.ReceivableId,
            value.AttemptCount,
            value.LastError,
            value.CreatedAtUtc,
            value.UpdatedAtUtc);
}
