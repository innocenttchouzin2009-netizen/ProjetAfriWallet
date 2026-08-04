using Microsoft.Extensions.Options;

namespace Subscriptions.Api.Configuration;

public sealed class MtnMomoOptionsValidation : IValidateOptions<MtnMomoOptions>
{
    public ValidateOptionsResult Validate(string? name, MtnMomoOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Options instance is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            return ValidateOptionsResult.Fail("Environment is required.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
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
