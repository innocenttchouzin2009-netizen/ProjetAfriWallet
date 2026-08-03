namespace IdentityService.Application.Services;

public sealed class AwidPolicy
{
    public TimeSpan AliasChangeCooldown { get; init; } = TimeSpan.FromDays(30);
    public TimeSpan PreviousAliasReservation { get; init; } = TimeSpan.FromDays(365);
    public string IssuedMarket { get; init; } = "237";
    public int PublicAwidRandomLength { get; init; } = 8;
    public int MaxCreateAttempts { get; init; } = 10;

    public IReadOnlySet<string> ProtectedAliases { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "support",
        "security",
        "afriwallet",
        "official",
        "verified",
        "bank",
        "government",
        "police",
        "visa",
        "mastercard",
        "paypal"
    };
}
