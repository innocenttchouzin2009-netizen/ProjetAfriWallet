using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace TreasuryDisasterRecovery.Backups;

public sealed class InMemoryTreasuryBackupProvider : ITreasuryBackupProvider
{
    private readonly ConcurrentDictionary<Guid, (TreasuryBackupSnapshot Metadata, byte[] Payload)> _snapshots = new();

    public Task<TreasuryBackupSnapshot> CreateBackupAsync(string backupReference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = System.Text.Encoding.UTF8.GetBytes($"AFW-TREASURY-BACKUP|{backupReference}|{DateTime.UtcNow:O}");
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var snapshot = new TreasuryBackupSnapshot(
            Guid.NewGuid(),
            backupReference,
            "SANDBOX",
            DateTime.UtcNow,
            "memory://treasury-backups",
            hash,
            payload.LongLength,
            "COMPLETED");

        _snapshots[snapshot.SnapshotId] = (snapshot, payload);

        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyCollection<TreasuryBackupSnapshot>> ListBackupsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<TreasuryBackupSnapshot>>(
            _snapshots.Values
                .Select(x => x.Metadata)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToArray());
    }

    public Task<byte[]> ReadBackupAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_snapshots.TryGetValue(snapshotId, out var item))
            throw new KeyNotFoundException("Backup snapshot not found.");

        return Task.FromResult(item.Payload);
    }
}