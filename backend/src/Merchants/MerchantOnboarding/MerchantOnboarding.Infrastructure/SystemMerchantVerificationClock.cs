using AfriWallet.Merchants.Onboarding.Application.Abstractions;

namespace AfriWallet.Merchants.Onboarding.Infrastructure;

public sealed class SystemMerchantVerificationClock : IMerchantVerificationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
