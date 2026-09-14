using AfriWallet.PaymentRequests.Application;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestInboxOutboxItemResponse(
    Guid Id,
    Guid RequesterWalletId,
    string RecipientKind,
    string CurrencyCode,
    long AmountMinor,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Status,
    DateTimeOffset? ClosedAtUtc);

public sealed record PaymentRequestInboxOutboxPageResponse(
    IReadOnlyList<PaymentRequestInboxOutboxItemResponse> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    bool HasMore);
