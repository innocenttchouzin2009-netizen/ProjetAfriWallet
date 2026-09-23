using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class MerchantRegistryDbContext(DbContextOptions<MerchantRegistryDbContext> options) : DbContext(options)
{
    public DbSet<MerchantRegistryEntity> Merchants => Set<MerchantRegistryEntity>();
    public DbSet<MerchantRegistryAuditEntity> AuditEvents => Set<MerchantRegistryAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantRegistryEntity>(entity =>
        {
            entity.ToTable("MerchantRegistry");
            entity.HasKey(x => x.MerchantId);
            entity.Property(x => x.MerchantId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OwnerAwid).HasMaxLength(128).IsRequired();
            entity.Property(x => x.OwnerAwidNormalized).HasMaxLength(128).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.LegalNameNormalized).HasMaxLength(256).IsRequired();
            entity.Property(x => x.TradingName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.SettlementCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.BusinessCategory).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(256).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(256);
            entity.Property(x => x.City).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(64);
            entity.Property(x => x.CapabilitiesCsv).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.OwnerAwidNormalized).IsUnique();
            entity.HasIndex(x => new { x.LegalNameNormalized, x.CountryCode }).IsUnique();
        });

        modelBuilder.Entity<MerchantRegistryAuditEntity>(entity =>
        {
            entity.ToTable("MerchantRegistryAudit");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.MerchantId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OwnerAwid).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(128).IsRequired();
            entity.Property(x => x.MetadataJson).IsRequired();
            entity.HasIndex(x => new { x.MerchantId, x.OccurredAtUtc });
        });
    }
}

public sealed class MerchantRegistryEntity
{
    public string MerchantId { get; set; } = string.Empty;
    public string OwnerAwid { get; set; } = string.Empty;
    public string OwnerAwidNormalized { get; set; } = string.Empty;
    public int Status { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string LegalNameNormalized { get; set; } = string.Empty;
    public string TradingName { get; set; } = string.Empty;
    public int MerchantType { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string SettlementCurrency { get; set; } = string.Empty;
    public string BusinessCategory { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CapabilitiesCsv { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
}

public sealed class MerchantRegistryAuditEntity
{
    public Guid EventId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string OwnerAwid { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
