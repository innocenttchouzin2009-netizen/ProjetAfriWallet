namespace IdentityService.Api.Fx;

public sealed record FxQuoteResponse(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    decimal Rate,
    DateTimeOffset QuotedAtUtc);

public sealed record FxErrorResponse(
    string Code,
    string Message,
    string TraceId);
