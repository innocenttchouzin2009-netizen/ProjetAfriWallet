namespace Operations.Domain;

public sealed class BackupSnapshot
{
    public Guid SnapshotId { get; init; } = Guid.NewGuid();

    public string StorageProvider { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool Encrypted { get; init; }

    public string Checksum { get; init; } = string.Empty;

    public BackupStatus Status { get; init; } = BackupStatus.Pending;
}