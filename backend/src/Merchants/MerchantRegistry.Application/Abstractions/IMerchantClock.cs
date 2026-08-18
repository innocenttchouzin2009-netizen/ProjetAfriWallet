namespace AfriWallet.Merchants.Registry.Application.Abstractions;

public interface IMerchantClock
{
    DateTimeOffset UtcNow { get; }
}
