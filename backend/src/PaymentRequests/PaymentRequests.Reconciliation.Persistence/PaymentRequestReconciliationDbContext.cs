using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Reconciliation.Persistence;

public sealed class PaymentRequestReconciliationDbContext(DbContextOptions<PaymentRequestReconciliationDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestReconciliationEntity> Records => Set<PaymentRequestReconciliationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<PaymentRequestReconciliationEntity>();
        record.ToTable("PaymentRequestReconciliations");
        record.HasKey(x => x.RequestId);
        record.Property(x => x.RequestId).IsRequired();
        record.Property(x => x.Status).IsRequired();
        record.HasIndex(x => x.TransferId).IsUnique();
    }
}

public sealed class PaymentRequestReconciliationEntity
{
    public Guid RequestId { get; set; }
    public int Status { get; set; }
    public Guid? TransferId { get; set; }
}
