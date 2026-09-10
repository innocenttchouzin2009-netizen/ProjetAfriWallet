namespace AfriWallet.Fx.Infrastructure;

public sealed record ConfiguredFxRate(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    decimal Rate);
