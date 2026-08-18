using AfriWallet.Disputes.Registry.Domain.Claims;
using AfriWallet.Disputes.Registry.Domain.Evidence;

namespace AfriWallet.Disputes.Registry.Api.Contracts;

public sealed record RegisterDisputeClaimRequest(
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
    string? MerchantReference);

public sealed record LinkDisputeEvidenceRequest(DisputeEvidenceType Type, string ReferenceId, string Summary);
public sealed record ResolveDisputeClaimRequest(string Outcome);
public sealed record RejectDisputeClaimRequest(string Reason);
public sealed record CancelDisputeClaimRequest(string Reason);
