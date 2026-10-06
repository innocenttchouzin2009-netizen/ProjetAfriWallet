using System.Text.Json;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Runtime;

public sealed class FileMobileMoneyPayoutStore : IMobileMoneyPayoutStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileMobileMoneyPayoutStore(string storePath)
    {
        if (string.IsNullOrWhiteSpace(storePath))
            throw new ArgumentException("Payout store path is required.", nameof(storePath));

        _storePath = Path.GetFullPath(storePath);
    }

    public async Task<MobileMoneyPayout?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var snapshots = await LoadAsync(cancellationToken);
            return snapshots
                .FirstOrDefault(snapshot =>
                    string.Equals(
                        snapshot.IdempotencyKey,
                        idempotencyKey.Trim(),
                        StringComparison.Ordinal))
                ?.ToDomain();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        MobileMoneyPayout payout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payout);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var snapshots = await LoadAsync(cancellationToken);
            var index = snapshots.FindIndex(snapshot =>
                string.Equals(
                    snapshot.IdempotencyKey,
                    payout.IdempotencyKey,
                    StringComparison.Ordinal));

            var snapshot = MobileMoneyPayoutSnapshot.FromDomain(payout);

            if (index >= 0)
            {
                if (snapshots[index].PayoutId != payout.PayoutId)
                {
                    throw new InvalidOperationException(
                        "Idempotency key is already associated with another payout.");
                }

                snapshots[index] = snapshot;
            }
            else
            {
                snapshots.Add(snapshot);
            }

            await SaveSnapshotsAsync(snapshots, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<MobileMoneyPayoutSnapshot>> LoadAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
            return [];

        await using var stream = File.OpenRead(_storePath);
        try
        {
            return await JsonSerializer.DeserializeAsync<List<MobileMoneyPayoutSnapshot>>(
                       stream,
                       SerializerOptions,
                       cancellationToken) ??
                   [];
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The mobile money payout store contains invalid JSON.",
                exception);
        }
    }

    private async Task SaveSnapshotsAsync(
        List<MobileMoneyPayoutSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{_storePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    snapshots,
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, _storePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
