using Microsoft.Extensions.Options;

namespace MobileMoney.Production.Configuration;

public sealed class MobileMoneyRateLimitOptionsValidator : IValidateOptions<MobileMoneyRateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, MobileMoneyRateLimitOptions options)
    {
        if (options.StatusPerIp.PermitLimit <= 0 || options.OperationsPerAwid.PermitLimit <= 0 || options.OperationsPerWallet.PermitLimit <= 0 || options.OperationsPerPhone.PermitLimit <= 0)
        {
            return ValidateOptionsResult.Fail("All permit limits must be greater than zero.");
        }

        if (options.ConnectorConcurrency.PermitLimit <= 0 || options.ConnectorConcurrency.QueueLimit < 0)
        {
            return ValidateOptionsResult.Fail("Connector concurrency limits must be valid.");
        }

        return ValidateOptionsResult.Success;
    }
}
