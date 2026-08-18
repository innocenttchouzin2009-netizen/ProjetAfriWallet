using AfriWallet.Disputes.Registry.Domain.Claims;
using AfriWallet.Disputes.Registry.Domain.Evidence;

namespace AfriWallet.Disputes.Registry.Application.Claims;

public sealed record RegisterDisputeClaimCommand(
    string Awid,
    Guid TransactionId,
    DisputeClaimType Type,
    string Reason,
    long AmountMinor,
    string Currency,
    string Description,
    DisputeSourceChannel SourceChannel,
    string? PaymentReference,
    string? BankTransferReference,
    string? MerchantReference,
    string Actor);

public sealed record LinkDisputeEvidenceCommand(
    Guid ClaimId,
    DisputeEvidenceType Type,
    string ReferenceId,
    string Summary,
    string Actor);

public sealed record ResolveDisputeClaimCommand(Guid ClaimId, string Outcome, string Actor);
public sealed record RejectDisputeClaimCommand(Guid ClaimId, string Reason, string Actor);
public sealed record CancelDisputeClaimCommand(Guid ClaimId, string Reason, string Actor);
