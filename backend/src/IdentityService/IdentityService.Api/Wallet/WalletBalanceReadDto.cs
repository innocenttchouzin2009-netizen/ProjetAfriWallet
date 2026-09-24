namespace IdentityService.Api.Wallet;

public sealed record WalletBalanceReadDto(
    Guid WalletId,
    string Currency,
    long AvailableMinor,
    string Status,
    string? CountryCode);
