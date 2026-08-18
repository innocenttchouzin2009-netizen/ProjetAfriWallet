namespace AfriWallet.Merchants.Onboarding.Application.Abstractions;

public interface IMerchantVerificationClock
{
    DateTimeOffset UtcNow { get; }
}
