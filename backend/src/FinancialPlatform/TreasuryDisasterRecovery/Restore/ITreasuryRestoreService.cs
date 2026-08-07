namespace TreasuryDisasterRecovery.Restore;

public interface ITreasuryRestoreService
{
    Task<RestoreResult> RestoreAsync(
        Guid snapshotId,
        CancellationToken cancellationToken);
}

public sealed record RestoreResult(
    Guid SnapshotId,
    bool Success,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    string Message);