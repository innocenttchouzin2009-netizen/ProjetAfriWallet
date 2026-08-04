using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MobileMoney.Production.Configuration;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddMobileMoneyRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MobileMoneyRateLimitOptions>()
            .Bind(configuration.GetSection(MobileMoneyRateLimitOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MobileMoneyRateLimitOptions>, MobileMoneyRateLimitOptionsValidator>();

        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                var policyName = context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown";
                var retryAfter = 30;
                await RateLimitRejectionWriter.WriteAsync(context.HttpContext, policyName, retryAfter);
            };

            options.AddPolicy(MobileMoneyRateLimitPolicies.StatusPerIp, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.StatusPerIp.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.StatusPerIp.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(MobileMoneyRateLimitPolicies.OperationsPerAwid, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                var awid = context.Request.Headers["X-Awid-ID"].ToString();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: string.IsNullOrWhiteSpace(awid) ? "unknown-awid" : awid,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.OperationsPerAwid.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.OperationsPerAwid.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(MobileMoneyRateLimitPolicies.OperationsPerWallet, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                var walletId = context.Request.Headers["X-Wallet-ID"].ToString();
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: string.IsNullOrWhiteSpace(walletId) ? "unknown-wallet" : walletId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.OperationsPerWallet.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.OperationsPerWallet.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(MobileMoneyRateLimitPolicies.OperationsPerPhone, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                var phone = context.Request.Headers["X-Phone-Number"].ToString();
                var partition = PhonePartitionHasher.Hash(phone);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partition,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.OperationsPerPhone.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.OperationsPerPhone.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(MobileMoneyRateLimitPolicies.ConnectorConcurrency, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                return RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: "mtn-connector",
                    factory: _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = opts.ConnectorConcurrency.PermitLimit,
                        QueueLimit = opts.ConnectorConcurrency.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MobileMoneyRateLimitPolicies.Callback, context =>
            {
                var opts = context.RequestServices.GetRequiredService<IOptions<MobileMoneyRateLimitOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "callback",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.Callback.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.Callback.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
