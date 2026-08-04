namespace MobileMoney.Production.Audit;

public sealed class AuditRetentionPolicy
{
    public int RetentionDays { get; init; } = 2555;
    public bool EnableExport { get; init; } = true;
    public bool ImmutableStorage { get; init; }
    public int CompressAfterDays { get; init; } = 30;
}
