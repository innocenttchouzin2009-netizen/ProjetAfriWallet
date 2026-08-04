namespace MobileMoney.Production.Configuration;

public sealed class ProductionEnvironmentGuard
{
    private readonly MtnMomoProductionOptions _options;

    public ProductionEnvironmentGuard(MtnMomoProductionOptions options)
    {
        _options = options;
    }

    public bool IsProductionAllowed()
    {
        return _options.EnableProduction && string.Equals(_options.Environment, "Production", StringComparison.OrdinalIgnoreCase);
    }

    public void EnsureProductionSafe()
    {
        if (IsProductionAllowed())
        {
            return;
        }

        // Production remains disabled by default until an explicit allow-list is activated.
    }
}
