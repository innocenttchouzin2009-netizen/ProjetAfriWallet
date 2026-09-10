using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.P2P.Application;

public sealed record ResolveRecipientRequest(RecipientReference Reference, Currency Currency);

public sealed record ResolvedRecipient(WalletId WalletId, Currency Currency, RecipientReference Reference);

public enum RecipientResolutionStatus
{
    Success = 1,
    NotFound = 2
}

public sealed record RecipientResolutionResult(RecipientResolutionStatus Status, ResolvedRecipient? Recipient)
{
    public static RecipientResolutionResult Found(ResolvedRecipient recipient) => new(RecipientResolutionStatus.Success, recipient);
    public static RecipientResolutionResult NotFound() => new(RecipientResolutionStatus.NotFound, null);
}
