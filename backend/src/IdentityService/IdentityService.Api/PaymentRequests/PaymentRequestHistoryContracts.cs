namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestHistoryItemResponse(
    Guid Id,
    string Direction,
    string RecipientKind,
    string CurrencyCode,
    long AmountMinor,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Status,
    DateTimeOffset? ClosedAtUtc);

public sealed record PaymentRequestHistoryPageResponse(
    IReadOnlyList<PaymentRequestHistoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    bool HasMore);
