namespace TreasuryDisasterRecovery.Backups;

public sealed record TreasuryBackupSnapshot(
    Guid SnapshotId,
    string BackupReference,
    string Environment,
    DateTime CreatedAtUtc,
    string StorageLocation,
    string Sha256,
    long SizeBytes,
    string Status);