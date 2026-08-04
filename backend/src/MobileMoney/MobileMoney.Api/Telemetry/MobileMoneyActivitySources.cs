using System.Diagnostics;

namespace MobileMoney.Production.Telemetry;

public static class MobileMoneyActivitySources
{
    public const string Deposit = "mobile-money.mtn-momo.deposit";
    public const string Withdrawal = "mobile-money.mtn-momo.withdrawal";
    public const string Status = "mobile-money.mtn-momo.status";
    public const string Callback = "mobile-money.mtn-momo.callback";
    public const string Token = "mobile-money.mtn-momo.token";
    public const string ProviderCall = "mobile-money.mtn-momo.provider-call";
}
