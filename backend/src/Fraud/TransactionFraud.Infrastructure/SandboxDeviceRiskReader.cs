using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Domain.Signals;

namespace AfriWallet.Fraud.TransactionFraud.Infrastructure;

public sealed class SandboxDeviceRiskReader : IDeviceRiskReader
{
    private readonly Dictionary<string, DeviceRiskSnapshot> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public void Set(DeviceRiskSnapshot snapshot)
    {
        _profiles[$"{snapshot.Awid}|{snapshot.DeviceId}"] = snapshot;
    }

    public Task<DeviceRiskSnapshot?> GetLatestAsync(string awid, string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _profiles.TryGetValue($"{awid}|{deviceId}", out var snapshot);
        return Task.FromResult(snapshot);
    }
}
