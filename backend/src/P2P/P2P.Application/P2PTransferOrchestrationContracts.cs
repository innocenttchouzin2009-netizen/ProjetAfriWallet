using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Application;

public sealed record ExecuteP2PTransferCommand(
    Guid SourceWalletId,
    RecipientReference Recipient,
    Currency Currency,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset RequestedAtUtc);

public sealed record P2PTransferReceipt(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);

public enum P2PTransferExecutionStatus
{
    Success = 1,
    RecipientNotFound = 2
}

public sealed record P2PTransferExecutionResult(
    P2PTransferExecutionStatus Status,
    ResolvedRecipient? Recipient,
    P2PTransferReceipt? Receipt)
{
    public static P2PTransferExecutionResult Succeeded(ResolvedRecipient recipient, P2PTransferReceipt receipt) =>
        new(P2PTransferExecutionStatus.Success, recipient, receipt);

    public static P2PTransferExecutionResult RecipientNotFound() =>
        new(P2PTransferExecutionStatus.RecipientNotFound, null, null);
}
