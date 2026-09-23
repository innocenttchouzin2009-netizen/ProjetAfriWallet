using AfriWallet.Merchants.PaymentIdentity.Application;
using AfriWallet.Merchants.PaymentIdentity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.PaymentIdentity.Infrastructure;

public sealed class EfMerchantPaymentIdentityRegistry(MerchantPaymentIdentityDbContext dbContext)
    : IMerchantPaymentIdentityRegistry
{
    public async Task AddAsync(MerchantPaymentIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        dbContext.Identities.Add(Map(identity));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MerchantPaymentIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var entity = await dbContext.Identities.SingleAsync(x => x.IdentityId == identity.IdentityId, cancellationToken);
        Copy(identity, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantPaymentIdentity?> GetByMerchantAfWalIdAsync(
        string merchantAfWalId,
        CancellationToken cancellationToken = default)
    {
        var normalized = MerchantPaymentIdentity.NormalizeMerchantAfWalId(merchantAfWalId);
        var entity = await dbContext.Identities.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MerchantAfWalId == normalized, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<MerchantPaymentIdentity?> GetByMerchantIdAsync(
        string merchantId,
        CancellationToken cancellationToken = default)
    {
        var normalized = MerchantPaymentIdentity.NormalizeMerchantId(merchantId);
        var entity = await dbContext.Identities.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MerchantId == normalized, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private static MerchantPaymentIdentityEntity Map(MerchantPaymentIdentity value)
    {
        var entity = new MerchantPaymentIdentityEntity { IdentityId = value.IdentityId };
        Copy(value, entity);
        return entity;
    }

    private static void Copy(MerchantPaymentIdentity value, MerchantPaymentIdentityEntity entity)
    {
        entity.MerchantAfWalId = value.MerchantAfWalId;
        entity.MerchantId = value.MerchantId;
        entity.WalletId = value.WalletId;
        entity.Status = (int)value.Status;
        entity.CreatedAtUtc = value.CreatedAtUtc;
        entity.UpdatedAtUtc = value.UpdatedAtUtc;
    }

    private static MerchantPaymentIdentity Map(MerchantPaymentIdentityEntity entity) =>
        MerchantPaymentIdentity.Restore(
            entity.IdentityId,
            entity.MerchantAfWalId,
            entity.MerchantId,
            entity.WalletId,
            (MerchantPaymentIdentityStatus)entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}

public sealed class EfMerchantPaymentIdentityAuditStore(MerchantPaymentIdentityDbContext dbContext)
    : IMerchantPaymentIdentityAuditStore
{
    public async Task AppendAsync(MerchantPaymentIdentityAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        dbContext.Audit.Add(new MerchantPaymentIdentityAuditEntity
        {
            EventId = entry.EventId,
            IdentityId = entry.IdentityId,
            MerchantAfWalId = entry.MerchantAfWalId,
            MerchantId = entry.MerchantId,
            WalletId = entry.WalletId,
            Operation = (int)entry.Operation,
            Actor = entry.Actor,
            OccurredAtUtc = entry.OccurredAtUtc,
            Detail = entry.Detail
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MerchantPaymentIdentityAuditEntry>> ListAsync(
        Guid identityId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Audit.AsNoTracking()
            .Where(x => x.IdentityId == identityId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new MerchantPaymentIdentityAuditEntry(
            x.EventId,
            x.IdentityId,
            x.MerchantAfWalId,
            x.MerchantId,
            x.WalletId,
            (MerchantPaymentIdentityAuditOperation)x.Operation,
            x.Actor,
            x.OccurredAtUtc,
            x.Detail)).ToArray();
    }
}
