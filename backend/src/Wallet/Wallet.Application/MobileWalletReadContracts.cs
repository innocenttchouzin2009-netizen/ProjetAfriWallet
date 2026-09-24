namespace AfriWallet.Wallet.Application;

public sealed record MobileWalletReadItem(
    Guid WalletId,
    string Currency,
    long AvailableMinor,
    string Status,
    string? CountryCode);

public sealed record MobileWalletReadResult(
    IReadOnlyList<MobileWalletReadItem> Wallets);
