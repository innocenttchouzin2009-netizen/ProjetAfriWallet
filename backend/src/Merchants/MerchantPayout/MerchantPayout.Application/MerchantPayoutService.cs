using AfriWallet.Merchants.Payout.Domain;

namespace AfriWallet.Merchants.Payout.Application;

public sealed record RegisterMerchantPayoutDestinationCommand(
    string MerchantId,
    MerchantPayoutDestinationType Type,
    string Reference,
    string Currency);

public sealed record ExecuteMerchantPayoutCommand(
    Guid ReceivableId,
    Guid DestinationId,
    string IdempotencyKey,
    string Actor);

public sealed record MerchantPayoutResult(
    Guid PayoutId,
    Guid ReceivableId,
    string MerchantId,
    long AmountMinor,
    string Currency,
    Guid DestinationId,
    MerchantPayoutStatus Status,
    string? ProviderReference,
    string? FailureCode,
    string IdempotencyKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class MerchantPayoutService(
    IMerchantReceivableStore receivables,
    IMerchantPayoutDestinationRepository destinations,
    IMerchantPayoutRepository payouts,
    IMerchantPayoutProvider provider,
    IMerchantPayoutAuditStore audit,
    TimeProvider timeProvider)
{
    public async Task<MerchantPayoutDestination> RegisterDestinationAsync(
        RegisterMerchantPayoutDestinationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var destination = MerchantPayoutDestination.Create(
            command.MerchantId,
            command.Type,
            command.Reference,
            command.Currency,
            timeProvider.GetUtcNow());
        await destinations.AddAsync(destination, cancellationToken);
        return destination;
    }

    public async Task<MerchantPayoutResult> ExecuteAsync(
        ExecuteMerchantPayoutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReceivableId == Guid.Empty) throw new ArgumentException("Receivable id is required.", nameof(command));
        if (command.DestinationId == Guid.Empty) throw new ArgumentException("Destination id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Idempotency key and actor are required.", nameof(command));

        var existingByKey = await payouts.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existingByKey is not null)
        {
            if (existingByKey.ReceivableId != command.ReceivableId)
                throw new InvalidOperationException("Idempotency key belongs to a different receivable.");
            return existingByKey.Status is MerchantPayoutStatus.Succeeded or MerchantPayoutStatus.Failed
                ? Map(existingByKey)
                : await SubmitAsync(existingByKey, command.Actor, cancellationToken);
        }

        var existingByReceivable = await payouts.GetByReceivableAsync(command.ReceivableId, cancellationToken);
        if (existingByReceivable is not null)
            return existingByReceivable.Status is MerchantPayoutStatus.Succeeded or MerchantPayoutStatus.Failed
                ? Map(existingByReceivable)
                : await SubmitAsync(existingByReceivable, command.Actor, cancellationToken);

        var receivable = await receivables.GetAsync(command.ReceivableId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant receivable not found.");

        EnsureReceivableEligible(receivable);

        var destination = await destinations.GetAsync(command.DestinationId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout destination not found.");

        if (!destination.Active) throw new InvalidOperationException("Merchant payout destination is disabled.");
        if (!string.Equals(destination.MerchantId, receivable.MerchantId, StringComparison.Ordinal))
            throw new InvalidOperationException("Payout destination belongs to a different merchant.");
        if (!string.Equals(destination.Currency, receivable.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException("Payout destination currency does not match receivable currency.");

        var payout = MerchantPayoutExecution.Create(
            receivable.ReceivableId,
            receivable.MerchantId,
            receivable.AmountMinor,
            receivable.Currency,
            destination.DestinationId,
            command.IdempotencyKey,
            timeProvider.GetUtcNow());

        await payouts.AddAsync(payout, cancellationToken);
        await WriteAudit(payout, destination, "payout.created", command.Actor, cancellationToken);
        return await SubmitAsync(payout, command.Actor, cancellationToken);
    }

    public async Task<MerchantPayoutResult> GetAsync(Guid payoutId, CancellationToken cancellationToken = default) =>
        Map(await payouts.GetAsync(payoutId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout not found."));

    private async Task<MerchantPayoutResult> SubmitAsync(
        MerchantPayoutExecution payout,
        string actor,
        CancellationToken cancellationToken)
    {
        var destination = await destinations.GetAsync(payout.DestinationId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout destination not found.");

        payout.Start(timeProvider.GetUtcNow());
        await payouts.SaveAsync(payout, cancellationToken);

        var result = await provider.ExecuteAsync(
            new MerchantPayoutProviderRequest(
                payout.PayoutId,
                payout.ReceivableId,
                payout.MerchantId,
                payout.AmountMinor,
                payout.Currency,
                destination.Type,
                destination.Reference,
                payout.IdempotencyKey),
            cancellationToken);

        if (result.Succeeded)
        {
            payout.Complete(
                result.ProviderReference ?? throw new InvalidOperationException("Payout provider reference is required."),
                timeProvider.GetUtcNow());
            await payouts.SaveAsync(payout, cancellationToken);
            await WriteAudit(payout, destination, "payout.completed", actor, cancellationToken);
        }
        else
        {
            payout.Fail(result.FailureCode ?? "provider_failure", timeProvider.GetUtcNow());
            await payouts.SaveAsync(payout, cancellationToken);
            await WriteAudit(payout, destination, "payout.failed", actor, cancellationToken);
        }

        return Map(payout);
    }

    private static void EnsureReceivableEligible(MerchantReceivableSnapshot receivable)
    {
        if (!string.Equals(receivable.CaptureStatus, "Captured", StringComparison.OrdinalIgnoreCase) ||
            !receivable.SettlementReady)
            throw new InvalidOperationException("Merchant receivable is not payout eligible.");
        if (receivable.AmountMinor <= 0)
            throw new InvalidOperationException("Merchant receivable amount must be positive.");
        if (string.IsNullOrWhiteSpace(receivable.MerchantId))
            throw new InvalidOperationException("Merchant receivable merchant id is required.");
        if (string.IsNullOrWhiteSpace(receivable.Currency) || receivable.Currency.Trim().Length != 3)
            throw new InvalidOperationException("Merchant receivable currency is invalid.");
    }

    private Task WriteAudit(
        MerchantPayoutExecution payout,
        MerchantPayoutDestination destination,
        string eventType,
        string actor,
        CancellationToken cancellationToken) =>
        audit.AppendAsync(
            new MerchantPayoutAuditEvent(
                Guid.NewGuid(),
                payout.PayoutId,
                payout.ReceivableId,
                payout.MerchantId,
                eventType,
                actor,
                timeProvider.GetUtcNow(),
                new Dictionary<string,string>
                {
                    ["status"] = payout.Status.ToString(),
                    ["destinationType"] = destination.Type.ToString(),
                    ["payoutExecutionPerformed"] = (payout.Status == MerchantPayoutStatus.Succeeded).ToString().ToLowerInvariant(),
                    ["realExternalPayoutPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false"
                }),
            cancellationToken);

    private static MerchantPayoutResult Map(MerchantPayoutExecution payout) =>
        new(
            payout.PayoutId,
            payout.ReceivableId,
            payout.MerchantId,
            payout.AmountMinor,
            payout.Currency,
            payout.DestinationId,
            payout.Status,
            payout.ProviderReference,
            payout.FailureCode,
            payout.IdempotencyKey,
            payout.CreatedAtUtc,
            payout.UpdatedAtUtc);
}
