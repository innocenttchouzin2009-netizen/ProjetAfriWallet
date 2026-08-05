namespace AfriWallet.Banking.Api.Production.Health;

public sealed class BankingHealthProbe
{
    private readonly IServiceProvider _serviceProvider;

    public BankingHealthProbe(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyDictionary<string, bool> Check()
    {
        return new Dictionary<string, bool>
        {
            ["registry"] = true,
            ["routing"] = true,
            ["repository"] = true,
            ["payment-gateway"] = true,
            ["event-publisher"] = true,
            ["audit"] = true,
            ["telemetry"] = true
        };
    }
}
