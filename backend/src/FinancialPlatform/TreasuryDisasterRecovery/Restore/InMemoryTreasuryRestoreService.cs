using TreasuryDisasterRecovery.Backups;
using TreasuryDisasterRecovery.Validation;

namespace TreasuryDisasterRecovery.Restore;

public sealed class InMemoryTreasuryRestoreService : ITreasuryRestoreService
{
    private readonly ITreasuryBackupProvider _backupProvider;
    private readonly BackupChecksumValidator _checksumValidator;

    public InMemoryTreasuryRestoreService(ITreasuryBackupProvider backupProvider, BackupChecksumValidator checksumValidator)
    {
        _backupProvider = backupProvider;
        _checksumValidator = checksumValidator;
    }

    public async Task<RestoreResult> RestoreAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;

        var backups = await _backupProvider.ListBackupsAsync(cancellationToken);
        var snapshot = backups.FirstOrDefault(x => x.SnapshotId == snapshotId)
            ?? throw new KeyNotFoundException("Backup snapshot not found.");

        var payload = await _backupProvider.ReadBackupAsync(snapshotId, cancellationToken);

        if (!_checksumValidator.Validate(snapshot, payload))
        {
            return new RestoreResult(snapshotId, false, started, DateTime.UtcNow, "Backup checksum validation failed.");
        }

        return new RestoreResult(snapshotId, true, started, DateTime.UtcNow, "Treasury restore completed.");
    }
}