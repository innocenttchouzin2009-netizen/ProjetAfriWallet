namespace AfriWallet.CardPlatform.Domain.Entities;

public sealed class CardLifecycle
{
    public string CardId { get; set; } = string.Empty;
    public string OwnerAwidId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string Status { get; set; } = "REQUESTED";
    public string? LastTransition { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class CardLifecycleRequest
{
    public string CardId { get; set; } = string.Empty;
    public string OwnerAwidId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
}
