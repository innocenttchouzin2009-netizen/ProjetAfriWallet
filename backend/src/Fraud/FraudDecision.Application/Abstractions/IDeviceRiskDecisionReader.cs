using AfriWallet.Fraud.Decision.Domain.Inputs;

namespace AfriWallet.Fraud.Decision.Application.Abstractions;

public interface IDeviceRiskDecisionReader
{
    Task<DeviceRiskInput?> GetLatestAsync(string awid, string deviceId, CancellationToken cancellationToken = default);
}