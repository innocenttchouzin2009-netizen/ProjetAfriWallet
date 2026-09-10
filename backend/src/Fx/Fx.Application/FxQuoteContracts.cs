namespace AfriWallet.Fx.Application;

public sealed record FxQuoteRequest(
    string SourceCurrencyCode,
    string TargetCurrencyCode);

public sealed record FxQuoteView(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    decimal Rate,
    DateTimeOffset QuotedAtUtc);

public sealed record FxQuoteOperationResult(
    bool Succeeded,
    FxQuoteView? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static FxQuoteOperationResult Success(FxQuoteView value) =>
        new(true, value, null, null);

    public static FxQuoteOperationResult Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}

public static class FxQuoteErrorCode
{
    public const string ValidationError = "FX_QUOTE_VALIDATION_ERROR";
    public const string QuoteUnavailable = "FX_QUOTE_UNAVAILABLE";
    public const string ProviderQuoteMismatch = "FX_PROVIDER_QUOTE_MISMATCH";
}
