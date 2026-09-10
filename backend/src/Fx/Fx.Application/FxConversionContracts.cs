namespace AfriWallet.Fx.Application;

public sealed record FxConversionCommand(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    long SourceAmountMinor,
    byte SourceMinorUnitDigits,
    byte TargetMinorUnitDigits,
    decimal Rate,
    DateTimeOffset QuotedAtUtc);

public sealed record FxConversionResult(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    long SourceAmountMinor,
    long TargetAmountMinor,
    byte SourceMinorUnitDigits,
    byte TargetMinorUnitDigits,
    decimal Rate,
    DateTimeOffset QuotedAtUtc,
    string RoundingMode);
