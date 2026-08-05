using Microsoft.Extensions.Options;

namespace AfriWallet.Banking.Api.Production.Configuration;

public sealed class BankingProductionOptionsValidator : IValidateOptions<BankingProductionOptions>
{
    public ValidateOptionsResult Validate(string? name, BankingProductionOptions options)
    {
        var failures = new List<string>();

        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Environment))
            {
                failures.Add("Banking production environment must be set.");
            }

            foreach (var setting in options.RequiredSettings)
            {
                if (string.IsNullOrWhiteSpace(setting))
                {
                    failures.Add($"Required configuration setting is missing: {setting}");
                }
            }

            foreach (var secret in options.RequiredSecrets)
            {
                if (string.IsNullOrWhiteSpace(secret))
                {
                    failures.Add($"Required secret is missing: {secret}");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
