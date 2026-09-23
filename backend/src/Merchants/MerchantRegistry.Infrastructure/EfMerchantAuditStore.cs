using System.Text.Json;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class EfMerchantAuditStore(MerchantRegistryDbContext dbContext) : IMerchantAuditStore
{
    public async Task AppendAsync(MerchantAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        dbContext.AuditEvents.Add(new MerchantRegistryAuditEntity
        {
            EventId = auditEvent.EventId,
            MerchantId = auditEvent.MerchantId.Trim().ToUpperInvariant(),
            OwnerAwid = auditEvent.OwnerAwid.Trim(),
            EventType = auditEvent.EventType.Trim(),
            Actor = auditEvent.Actor.Trim(),
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MerchantAuditEvent>> GetAsync(
        string merchantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        var normalized = merchantId.Trim().ToUpperInvariant();
        var rows = await dbContext.AuditEvents.AsNoTracking()
            .Where(x => x.MerchantId == normalized)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new MerchantAuditEvent(
                x.EventId,
                x.MerchantId,
                x.OwnerAwid,
                x.EventType,
                x.Actor,
                x.OccurredAtUtc,
                JsonSerializer.Deserialize<Dictionary<string, string>>(x.MetadataJson) ?? new Dictionary<string, string>()))
            .ToArray();
    }
}
