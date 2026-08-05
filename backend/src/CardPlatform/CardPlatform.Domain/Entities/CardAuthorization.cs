namespace AfriWallet.CardPlatform.Domain.Entities;

public sealed class CardAuthorization
{
    public string AuthorizationId { get; set; } = Guid.NewGuid().ToString("N");
    public string CardId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = "XAF";
    public string MerchantCategoryCode { get; set; } = string.Empty;
    public string MerchantCountry { get; set; } = string.Empty;
    public string Channel { get; set; } = "online";
    public string Decision { get; set; } = "DECLINED";
    public string ReasonCode { get; set; } = "UNKNOWN";
    public long ApprovedAmountMinor { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public long DurationMs { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class CardAuthorizationRequest
{
    public string CardId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = "XAF";
    public string MerchantCategoryCode { get; set; } = string.Empty;
    public string MerchantCountry { get; set; } = string.Empty;
    public string Channel { get; set; } = "online";
    public Dictionary<string, object?> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CardAuthorizationReverseRequest
{
    public string AuthorizationId { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = "REVERSED";
}
