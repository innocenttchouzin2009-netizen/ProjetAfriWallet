namespace AfriWallet.Wallet.Persistence;

public sealed class WalletEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
