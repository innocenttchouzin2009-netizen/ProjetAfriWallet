namespace IdentityService.Api.Wallet;

public sealed record CreateWalletRequest(string CurrencyCode, string? CountryCode = null);

public sealed record WalletErrorResponse(string Code, string Message, string TraceId);
