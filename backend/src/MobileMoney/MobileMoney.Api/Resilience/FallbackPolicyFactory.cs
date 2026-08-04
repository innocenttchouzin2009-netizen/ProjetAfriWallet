namespace MobileMoney.Production.Resilience;

public static class FallbackPolicyFactory
{
    public static object BuildFallback(string providerName)
    {
        return new { provider = providerName, status = "degraded", message = "Fallback response issued by resilience pipeline" };
    }
}
