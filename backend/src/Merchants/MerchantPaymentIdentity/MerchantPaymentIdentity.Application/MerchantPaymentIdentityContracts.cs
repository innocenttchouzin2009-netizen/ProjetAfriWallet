using AfriWallet.Merchants.PaymentIdentity.Domain;

namespace AfriWallet.Merchants.PaymentIdentity.Application;

public enum MerchantPaymentIdentityAuditOperation
{
    Registered = 1,
    Disabled = 2,
    Resolved = 3
}

public sealed record MerchantPaymentIdentityAuditEntry(
    Guid EventId,
    Guid IdentityId,
    string MerchantAfWalId,
    string MerchantId,
    Guid WalletId,
    MerchantPaymentIdentityAuditOperation Operation,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    string? Detail);

public sealed record MerchantPaymentIdentityResolution(
    string MerchantAfWalId,
    string MerchantId,
    Guid WalletId);

public interface IMerchantPaymentIdentityRegistry
{
    Task AddAsync(MerchantPaymentIdentity identity, CancellationToken cancellationToken = default);
    Task UpdateAsync(MerchantPaymentIdentity identity, CancellationToken cancellationToken = default);
    Task<MerchantPaymentIdentity?> GetByMerchantAfWalIdAsync(string merchantAfWalId, CancellationToken cancellationToken = default);
    Task<MerchantPaymentIdentity?> GetByMerchantIdAsync(string merchantId, CancellationToken cancellationToken = default);
}

public interface IMerchantPaymentIdentityAuditStore
{
    Task AppendAsync(MerchantPaymentIdentityAuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MerchantPaymentIdentityAuditEntry>> ListAsync(Guid identityId, CancellationToken cancellationToken = default);
}

public interface IMerchantPaymentIdentityResolver
{
    Task<MerchantPaymentIdentityResolution?> ResolveAsync(
        string merchantAfWalId,
        string actor,
        CancellationToken cancellationToken = default);
}
