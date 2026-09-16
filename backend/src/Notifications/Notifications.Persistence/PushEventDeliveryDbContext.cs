using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class PushEventDeliveryDbContext(DbContextOptions<PushEventDeliveryDbContext> options) : DbContext(options)
{
    public DbSet<PushEventDeliveryEntity> Deliveries => Set<PushEventDeliveryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PushEventDeliveryEntity>();
        entity.ToTable("PushEventDeliveries");
        entity.HasKey(x => new { x.EventId, x.RegistrationId });
        entity.Property(x => x.EventId).IsRequired();
        entity.Property(x => x.RegistrationId).IsRequired();
        entity.Property(x => x.UserId).IsRequired();
        entity.Property(x => x.AttemptCount).IsRequired();
        entity.Property(x => x.State).IsRequired();
        entity.Property(x => x.LastAttemptAtUtc).HasMaxLength(64).IsRequired();
        entity.Property(x => x.NextAttemptAtUtc).HasMaxLength(64);
        entity.HasIndex(x => x.EventId);
    }
}

public sealed class PushEventDeliveryEntity
{
    public Guid EventId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public int AttemptCount { get; set; }
    public int State { get; set; }
    public string LastAttemptAtUtc { get; set; } = string.Empty;
    public string? NextAttemptAtUtc { get; set; }
}

public sealed class EfPushEventDeliveryRepository(PushEventDeliveryDbContext dbContext) : IPushEventDeliveryRepository
{
    public async Task<IReadOnlyList<PushEventDeliveryRecord>> ListByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        var rows = await dbContext.Deliveries.AsNoTracking()
            .Where(x => x.EventId == eventId)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRecord).ToArray();
    }

    public async Task UpsertAsync(PushEventDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var entity = await dbContext.Deliveries.FindAsync([record.EventId, record.RegistrationId.Value], cancellationToken);
        if (entity is null)
        {
            entity = new PushEventDeliveryEntity { EventId = record.EventId, RegistrationId = record.RegistrationId.Value };
            dbContext.Deliveries.Add(entity);
        }
        entity.UserId = record.UserId;
        entity.AttemptCount = record.AttemptCount;
        entity.State = (int)record.State;
        entity.LastAttemptAtUtc = record.LastAttemptAtUtc.ToString("O");
        entity.NextAttemptAtUtc = record.NextAttemptAtUtc?.ToString("O");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PushEventDeliveryRecord ToRecord(PushEventDeliveryEntity entity) => new(
        entity.EventId,
        entity.UserId,
        PushDeviceRegistrationId.From(entity.RegistrationId),
        entity.AttemptCount,
        (PushEventDeliveryState)entity.State,
        DateTimeOffset.Parse(entity.LastAttemptAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
        entity.NextAttemptAtUtc is null ? null : DateTimeOffset.Parse(entity.NextAttemptAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
}
