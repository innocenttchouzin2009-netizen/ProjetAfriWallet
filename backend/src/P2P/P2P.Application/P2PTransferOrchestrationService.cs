namespace AfriWallet.P2P.Application;

public sealed class P2PTransferOrchestrationService(
    RecipientResolutionService recipientResolutionService,
    IP2PTransferPort transferPort)
{
    public async Task<P2PTransferExecutionResult> ExecuteAsync(
        ExecuteP2PTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Recipient);
        ArgumentNullException.ThrowIfNull(command.Currency);

        if (command.SourceWalletId == Guid.Empty)
        {
            throw new ArgumentException("Source wallet id cannot be empty.", nameof(command.SourceWalletId));
        }

        if (command.AmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.AmountMinor), "P2P transfer amount must be positive.");
        }

        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(command.CorrelationId));
        }

        if (command.RequestedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("P2P transfer timestamp must be UTC.", nameof(command.RequestedAtUtc));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var resolution = await recipientResolutionService.ResolveAsync(
            new ResolveRecipientRequest(command.Recipient, command.Currency),
            cancellationToken);

        if (resolution.Status == RecipientResolutionStatus.NotFound || resolution.Recipient is null)
        {
            return P2PTransferExecutionResult.RecipientNotFound();
        }

        var receipt = await transferPort.ExecuteAsync(
            command.SourceWalletId,
            resolution.Recipient.WalletId.Value,
            command.AmountMinor,
            command.CorrelationId,
            command.RequestedAtUtc,
            cancellationToken);

        return P2PTransferExecutionResult.Succeeded(resolution.Recipient, receipt);
    }
}
