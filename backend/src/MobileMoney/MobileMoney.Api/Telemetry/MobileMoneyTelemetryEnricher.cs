using System.Diagnostics;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Telemetry;

public sealed class MobileMoneyTelemetryEnricher
{
    public void Enrich(Activity? activity, HttpContext? httpContext)
    {
        if (activity is null)
        {
            return;
        }

        var correlation = CorrelationContext.FromHttpContext(httpContext);
        if (correlation is not null)
        {
            activity.SetTag("afw.correlation.id", correlation.CorrelationId);
            activity.SetTag("afw.transaction.id", correlation.TransactionId);
            activity.SetTag("afw.provider.reference", correlation.ProviderReference);
        }
    }
}
