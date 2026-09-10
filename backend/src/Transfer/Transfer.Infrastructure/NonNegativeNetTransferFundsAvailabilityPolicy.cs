using AfriWallet.Transfer.Application;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class NonNegativeNetTransferFundsAvailabilityPolicy : ITransferFundsAvailabilityPolicy
{
    public long GetAvailableMinor(TransferBalanceProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return Math.Max(0, projection.NetMinor);
    }
}
