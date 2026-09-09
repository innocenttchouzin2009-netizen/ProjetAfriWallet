using AfriWallet.Wallet.Domain;

namespace AfriWallet.Wallet.Application;

public sealed record CreateWalletCommand(Guid OwnerId, string CurrencyCode, string? CountryCode = null);

public sealed record WalletView(
    Guid WalletId,
    Guid OwnerId,
    string CurrencyCode,
    string? CountryCode,
    WalletStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WalletOperationResult<T>(bool Succeeded, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static WalletOperationResult<T> Success(T value) => new(true, value, null, null);
    public static WalletOperationResult<T> Failure(string code, string message) => new(false, default, code, message);
}

public static class WalletErrorCode
{
    public const string ValidationError = "WALLET_VALIDATION_ERROR";
    public const string UnsupportedCurrency = "WALLET_UNSUPPORTED_CURRENCY";
    public const string DuplicateWallet = "WALLET_DUPLICATE";
    public const string NotFound = "WALLET_NOT_FOUND";
    public const string InvalidTransition = "WALLET_INVALID_TRANSITION";
}
