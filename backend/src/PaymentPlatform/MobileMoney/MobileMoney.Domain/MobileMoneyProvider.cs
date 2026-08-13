namespace AfriWallet.PaymentPlatform.MobileMoney.Domain;

public sealed record MobileMoneyProvider(
    string Code,
    string Name,
    IReadOnlySet<string> Countries,
    IReadOnlySet<string> Currencies,
    bool Enabled = true);