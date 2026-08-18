namespace AfriWallet.Disputes.Registry.Domain.Claims;

/// External identifiers are stored as opaque references; the registry never calls the owning platforms.
public sealed record DisputeClaimReferences(
    string? PaymentReference,
    string? BankTransferReference,
    string? MerchantReference);
