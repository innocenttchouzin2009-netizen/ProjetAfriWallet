namespace AfriWallet.Merchant.Domain.Entities;

public enum PosTerminalStatus
{
    Active,
    Inactive,
    Maintenance
}

public enum PosChannel
{
    QrCode,
    WebCheckout,
    CashierInterface,
    SoftPos,
    DedicatedPosTerminal,
    ApiPartner
}

public enum PosTransactionStatus
{
    Initiated,
    Processing,
    Completed,
    Failed
}

public sealed class PosTerminal
{
    public string TerminalId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string TerminalCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PosTerminalStatus Status { get; set; } = PosTerminalStatus.Active;
    public List<string> Capabilities { get; set; } = [];
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset? LastHeartbeatUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;
}
