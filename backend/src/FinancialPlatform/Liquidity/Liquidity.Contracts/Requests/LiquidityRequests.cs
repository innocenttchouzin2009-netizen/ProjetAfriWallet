namespace Liquidity.Contracts.Requests;

public sealed record RebalanceLiquidityRequest(
    string CurrencyCode,
    long MinimumMinor,
    long WarningMinor,
    long CriticalMinor);
