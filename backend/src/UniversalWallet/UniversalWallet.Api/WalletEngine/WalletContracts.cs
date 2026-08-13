namespace UniversalWallet.Api.WalletEngine;

public sealed class CreateWalletRequest
{
    public string Awid { get; init; } = string.Empty;
    public WalletType WalletType { get; init; } = WalletType.Personal;
    public string Currency { get; init; } = "EUR";
}

public sealed class UpdateWalletStatusRequest
{
    public WalletStatus Status { get; init; }
}

public sealed class WalletResponse
{
    public Guid Id { get; init; }
    public string WalletNumber { get; init; } = string.Empty;
    public string Awid { get; init; } = string.Empty;
    public string WalletType { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal AvailableBalance { get; init; }
    public decimal PendingBalance { get; init; }
    public decimal ReservedBalance { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
