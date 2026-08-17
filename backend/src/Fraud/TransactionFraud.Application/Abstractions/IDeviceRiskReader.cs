using AfriWallet.Fraud.TransactionFraud.Domain.Signals;

namespace AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

public interface IDeviceRiskReader
{
    Task<DeviceRiskSnapshot?> GetLatestAsync(
        string awid,
        string deviceId,
        CancellationToken cancellationToken = default);
}
