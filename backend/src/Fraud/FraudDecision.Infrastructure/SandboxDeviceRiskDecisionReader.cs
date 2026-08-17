using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Domain.Inputs;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class SandboxDeviceRiskDecisionReader : IDeviceRiskDecisionReader
{
    private readonly Dictionary<string, DeviceRiskInput> items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(DeviceRiskInput input) => items[$"{input.Awid}|{input.DeviceId}"] = input;

    public Task<DeviceRiskInput?> GetLatestAsync(string awid, string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue($"{awid}|{deviceId}", out var input);
        return Task.FromResult(input);
    }
}