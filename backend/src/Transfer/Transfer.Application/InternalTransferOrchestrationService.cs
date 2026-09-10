namespace AfriWallet.Transfer.Application;

public sealed class InternalTransferOrchestrationService(
    ITransferWalletReader walletReader,
    ITransferBalanceReader balanceReader,
    ITransferLedgerPort ledgerPort,
    InternalTransferPlanningService planningService)
{
    public async Task<InternalTransferExecutionResult> ExecuteAsync(
        ExecuteInternalTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SourceWalletId == Guid.Empty)
        {
            throw new ArgumentException("Source wallet id cannot be empty.", nameof(command.SourceWalletId));
        }

        if (command.TargetWalletId == Guid.Empty)
        {
            throw new ArgumentException("Target wallet id cannot be empty.", nameof(command.TargetWalletId));
        }

        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(command.CorrelationId));
        }

        if (await ledgerPort.ExistsByCorrelationIdAsync(command.CorrelationId, cancellationToken))
        {
            throw new InvalidOperationException("A ledger journal already exists for this transfer correlation id.");
        }

        var source = await walletReader.GetAsync(command.SourceWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Source wallet was not found.");
        var target = await walletReader.GetAsync(command.TargetWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Target wallet was not found.");

        var availableMinor = await balanceReader.GetAvailableMinorAsync(
            source.AccountId,
            source.CurrencyCode,
            cancellationToken);

        var plan = planningService.Prepare(new PrepareInternalTransferCommand(
            new TransferWalletContext(source.WalletId, source.AccountId, source.CurrencyCode, source.IsActive, availableMinor),
            new TransferWalletContext(target.WalletId, target.AccountId, target.CurrencyCode, target.IsActive, 0),
            command.AmountMinor,
            command.CorrelationId,
            command.RequestedAtUtc));

        await ledgerPort.PostAsync(plan.JournalEntry, cancellationToken);
        return new InternalTransferExecutionResult(plan.Intent, plan.JournalEntry);
    }
}
