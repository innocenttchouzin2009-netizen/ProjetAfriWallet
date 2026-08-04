using Microsoft.Extensions.Options;

namespace MobileMoney.Production.Configuration;

public sealed class MtnMomoOptionsValidator : IValidateOptions<MtnMomoProductionOptions>
{
    public ValidateOptionsResult Validate(string? name, MtnMomoProductionOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Options instance is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            return ValidateOptionsResult.Fail("Environment is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("BaseUrl must be a valid absolute URI.");
        }

        if (options.TimeoutSeconds <= 0 || options.TimeoutSeconds > 300)
        {
            return ValidateOptionsResult.Fail("TimeoutSeconds must be between 1 and 300.");
        }

        return ValidateOptionsResult.Success;
    }
}
