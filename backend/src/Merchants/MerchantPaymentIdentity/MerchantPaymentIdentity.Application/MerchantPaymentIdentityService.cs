using AfriWallet.Merchants.PaymentIdentity.Domain;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Merchants.PaymentIdentity.Application;

public sealed class MerchantPaymentIdentityService(
    IMerchantPaymentIdentityRegistry registry,
    IMerchantPaymentIdentityAuditStore auditStore,
    IMerchantRepository merchantRepository,
    IWalletRepository walletRepository,
    TimeProvider timeProvider)
    : IMerchantPaymentIdentityResolver
{
    public async Task<MerchantPaymentIdentity> RegisterAsync(
        string merchantAfWalId,
        string merchantId,
        Guid walletId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var normalizedActor = NormalizeActor(actor);
        var canonicalAfWalId = MerchantPaymentIdentity.NormalizeMerchantAfWalId(merchantAfWalId);
        var canonicalMerchantId = MerchantPaymentIdentity.NormalizeMerchantId(merchantId);
        var typedMerchantId = new MerchantId(canonicalMerchantId);

        var merchant = await merchantRepository.GetAsync(typedMerchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant was not found.");
        if (merchant.Status != MerchantStatus.Active)
            throw new InvalidOperationException("Merchant must be active before a payment identity can be registered.");

        var wallet = await walletRepository.GetAsync(WalletId.From(walletId), cancellationToken)
            ?? throw new KeyNotFoundException("Wallet was not found.");
        if (wallet.Status != WalletStatus.Active)
            throw new InvalidOperationException("Wallet must be active before it can receive a merchant payment identity.");

        if (await registry.GetByMerchantAfWalIdAsync(canonicalAfWalId, cancellationToken) is not null)
            throw new InvalidOperationException("Merchant AfWal ID is already registered.");
        if (await registry.GetByMerchantIdAsync(canonicalMerchantId, cancellationToken) is not null)
            throw new InvalidOperationException("Merchant already has a payment identity.");

        var now = timeProvider.GetUtcNow();
        var identity = MerchantPaymentIdentity.Create(canonicalAfWalId, canonicalMerchantId, walletId, now);
        await registry.AddAsync(identity, cancellationToken);
        await WriteAuditAsync(identity, MerchantPaymentIdentityAuditOperation.Registered, normalizedActor, now, null, cancellationToken);
        return identity;
    }

    public async Task<MerchantPaymentIdentityResolution?> ResolveAsync(
        string merchantAfWalId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var normalizedActor = NormalizeActor(actor);
        var identity = await registry.GetByMerchantAfWalIdAsync(merchantAfWalId, cancellationToken);
        if (identity is null || identity.Status != MerchantPaymentIdentityStatus.Active)
            return null;

        var merchant = await merchantRepository.GetAsync(new MerchantId(identity.MerchantId), cancellationToken);
        if (merchant is null || merchant.Status != MerchantStatus.Active)
            return null;

        var wallet = await walletRepository.GetAsync(WalletId.From(identity.WalletId), cancellationToken);
        if (wallet is null || wallet.Status != WalletStatus.Active)
            return null;

        var now = timeProvider.GetUtcNow();
        await WriteAuditAsync(identity, MerchantPaymentIdentityAuditOperation.Resolved, normalizedActor, now, null, cancellationToken);
        return new(identity.MerchantAfWalId, identity.MerchantId, identity.WalletId);
    }

    public async Task<MerchantPaymentIdentity> DisableAsync(
        string merchantAfWalId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var normalizedActor = NormalizeActor(actor);
        var identity = await registry.GetByMerchantAfWalIdAsync(merchantAfWalId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payment identity was not found.");

        var now = timeProvider.GetUtcNow();
        identity.Disable(now);
        await registry.UpdateAsync(identity, cancellationToken);
        await WriteAuditAsync(identity, MerchantPaymentIdentityAuditOperation.Disabled, normalizedActor, now, null, cancellationToken);
        return identity;
    }

    public Task<IReadOnlyList<MerchantPaymentIdentityAuditEntry>> GetAuditAsync(
        Guid identityId,
        CancellationToken cancellationToken = default) =>
        auditStore.ListAsync(identityId, cancellationToken);

    private Task WriteAuditAsync(
        MerchantPaymentIdentity identity,
        MerchantPaymentIdentityAuditOperation operation,
        string actor,
        DateTimeOffset atUtc,
        string? detail,
        CancellationToken cancellationToken) =>
        auditStore.AppendAsync(
            new MerchantPaymentIdentityAuditEntry(
                Guid.NewGuid(),
                identity.IdentityId,
                identity.MerchantAfWalId,
                identity.MerchantId,
                identity.WalletId,
                operation,
                actor,
                atUtc,
                detail),
            cancellationToken);

    private static string NormalizeActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));
        var normalized = actor.Trim();
        if (normalized.Length > 256)
            throw new ArgumentException("Actor cannot exceed 256 characters.", nameof(actor));
        return normalized;
    }
}
