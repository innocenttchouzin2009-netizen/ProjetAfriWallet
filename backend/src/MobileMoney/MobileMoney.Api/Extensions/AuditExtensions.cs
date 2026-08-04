using Microsoft.Extensions.Options;
using MobileMoney.Production.Audit;

namespace MobileMoney.Production.Extensions;

public static class AuditExtensions
{
    public static IServiceCollection AddMobileMoneyAudit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuditOptions>()
            .Bind(configuration.GetSection(AuditOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<AuditRepository>();
        services.AddSingleton<IAuditService, AuditService>();
        services.AddSingleton<AuditExportService>();
        services.AddSingleton<AuditSearchService>();
        return services;
    }
}
