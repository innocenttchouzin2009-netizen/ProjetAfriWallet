namespace IdentityService.Api.Wallet;

public sealed record CreateWalletRequest(string CurrencyCode, string? CountryCode = null);

public sealed record WalletErrorResponse(string Code, string Message, string TraceId);

public sealed record WalletOverviewItemResponse(
    Guid WalletId,
    string Currency,
    long AvailableMinor,
    string Status,
    string? CountryCode);

public sealed record WalletOverviewResponse(
    IReadOnlyList<WalletOverviewItemResponse> Wallets);
