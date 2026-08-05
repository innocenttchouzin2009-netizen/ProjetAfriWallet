namespace AfriWallet.CardPlatform.Domain.Entities;

public sealed class CardToken
{
    public string TokenId { get; set; } = Guid.NewGuid().ToString("N");
    public string CardId { get; set; } = string.Empty;
    public string OwnerAwidId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string Network { get; set; } = "Visa";
    public string TokenReference { get; set; } = string.Empty;
    public string TokenType { get; set; } = "NETWORK_TOKEN";
    public string Status { get; set; } = "REQUESTED";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class CardTokenRequest
{
    public string CardId { get; set; } = string.Empty;
    public string OwnerAwidId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string Network { get; set; } = "Visa";
    public string TokenType { get; set; } = "NETWORK_TOKEN";
}
