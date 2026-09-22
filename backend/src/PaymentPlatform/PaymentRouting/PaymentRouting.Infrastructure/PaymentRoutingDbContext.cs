using Microsoft.EntityFrameworkCore;

namespace PaymentRouting.Infrastructure.Persistence;

public sealed class PaymentRoutingDbContext(DbContextOptions<PaymentRoutingDbContext> options) : DbContext(options)
{
    public DbSet<PaymentProviderEntity> Providers => Set<PaymentProviderEntity>();
    public DbSet<RoutingDecisionEntity> Decisions => Set<RoutingDecisionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentProviderEntity>(entity =>
        {
            entity.ToTable("PaymentRoutingProviders");
            entity.HasKey(x => x.ProviderId);
            entity.Property(x => x.ProviderId).HasMaxLength(128);
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CountriesCsv).IsRequired();
            entity.Property(x => x.CurrenciesCsv).IsRequired();
            entity.HasIndex(x => new { x.Rail, x.Status });
        });

        modelBuilder.Entity<RoutingDecisionEntity>(entity =>
        {
            entity.ToTable("PaymentRoutingDecisions");
            entity.HasKey(x => x.DecisionId);
            entity.HasIndex(x => x.PaymentIntentId).IsUnique();
            entity.Property(x => x.SelectedProviderId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AlternativesJson).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(512).IsRequired();
        });
    }
}

public sealed class PaymentProviderEntity
{
    public string ProviderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Rail { get; set; }
    public string CountriesCsv { get; set; } = string.Empty;
    public string CurrenciesCsv { get; set; } = string.Empty;
    public decimal BaseCostScore { get; set; }
    public int Priority { get; set; }
    public int Status { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class RoutingDecisionEntity
{
    public Guid DecisionId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string SelectedProviderId { get; set; } = string.Empty;
    public int SelectedRail { get; set; }
    public decimal SelectedScore { get; set; }
    public decimal SelectedCostScore { get; set; }
    public double SelectedSuccessRate { get; set; }
    public double SelectedAverageLatencyMs { get; set; }
    public int SelectedPriority { get; set; }
    public string AlternativesJson { get; set; } = "[]";
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
