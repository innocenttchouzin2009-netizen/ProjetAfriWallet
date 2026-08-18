using AfriWallet.Merchants.Registry.Application.Abstractions;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class SystemMerchantClock : IMerchantClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
