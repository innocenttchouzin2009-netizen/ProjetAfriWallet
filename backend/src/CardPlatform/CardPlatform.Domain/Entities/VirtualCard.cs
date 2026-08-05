namespace AfriWallet.CardPlatform.Domain.Entities;

public sealed class VirtualCard
{
    public string VirtualCardId { get; set; } = Guid.NewGuid().ToString("N");
    public string CardProgramId { get; set; } = string.Empty;
    public string OwnerAwidId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public string CardToken { get; set; } = string.Empty;
    public string MaskedPan { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; } = 12;
    public int ExpiryYear { get; set; } = DateTime.UtcNow.Year + 3;
    public string Status { get; set; } = "REQUESTED";
    public long SpendingLimitMinor { get; set; } = 0;
    public long DailyLimitMinor { get; set; } = 0;
    public long MonthlyLimitMinor { get; set; } = 0;
    public string BaseCurrency { get; set; } = "XAF";
    public List<string> AllowedCurrencies { get; set; } = ["XAF"];
    public bool EcommerceEnabled { get; set; } = true;
    public bool ContactlessEnabled { get; set; } = true;
    public bool InternationalEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? FrozenAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public int Version { get; set; } = 1;
}
