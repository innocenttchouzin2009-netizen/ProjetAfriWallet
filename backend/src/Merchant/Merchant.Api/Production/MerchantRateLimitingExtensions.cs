using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace AfriWallet.Merchant.Api.Production;

public static class MerchantRateLimitingExtensions
{
    public static IServiceCollection AddMerchantRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("merchant-registry", limiterOptions =>
            {
                limiterOptions.PermitLimit = 50;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("onboarding", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("qr", limiterOptions =>
            {
                limiterOptions.PermitLimit = 30;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("pos", limiterOptions =>
            {
                limiterOptions.PermitLimit = 40;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("settlement", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("dashboard", limiterOptions =>
            {
                limiterOptions.PermitLimit = 30;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
        });

        return services;
    }
}
