namespace TreasuryDisasterRecovery.Backups;

public interface ITreasuryBackupProvider
{
    Task<TreasuryBackupSnapshot> CreateBackupAsync(
        string backupReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TreasuryBackupSnapshot>> ListBackupsAsync(
        CancellationToken cancellationToken);

    Task<byte[]> ReadBackupAsync(
        Guid snapshotId,
        CancellationToken cancellationToken);
}