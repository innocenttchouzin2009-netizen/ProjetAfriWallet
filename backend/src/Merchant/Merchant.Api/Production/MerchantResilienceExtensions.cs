using Polly;
using Polly.Extensions.Http;

namespace AfriWallet.Merchant.Api.Production;

public static class MerchantResilienceExtensions
{
    public static IServiceCollection AddMerchantResilience(this IServiceCollection services)
    {
        services.AddHttpClient("merchant-external")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10));
}
