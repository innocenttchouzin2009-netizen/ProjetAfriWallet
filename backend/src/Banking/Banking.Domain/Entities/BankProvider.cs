namespace AfriWallet.Banking.Domain.Entities;

public sealed class BankProvider
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public IReadOnlyCollection<string> SupportedCurrencies { get; init; } = Array.Empty<string>();
    public string? SwiftCode { get; init; }
    public string? Bic { get; init; }
    public string? NationalClearingCode { get; init; }
    public IReadOnlyCollection<string> TransferSchemes { get; init; } = Array.Empty<string>();
    public bool SupportsSepa { get; init; }
    public bool SupportsSwift { get; init; }
    public bool SupportsInstantPayments { get; init; }
    public bool SupportsDomesticTransfers { get; init; }
    public string SettlementWindow { get; init; } = "T+1";
    public string CutoffTime { get; init; } = "17:00 UTC";
    public string EstimatedDelivery { get; init; } = "1 business day";
    public int EstimatedDeliveryDays { get; init; } = 1;
    public decimal MinimumAmountMinor { get; init; }
    public decimal MaximumAmountMinor { get; init; } = 100_000_000m;
    public decimal FixedFeeMinor { get; init; }
    public decimal PercentageFee { get; init; }
    public string Environment { get; init; } = "Sandbox";
    public string Status { get; init; } = "Active";
    public int Priority { get; init; } = 100;
    public bool MaintenanceMode { get; init; }
    public IReadOnlyCollection<string> Capabilities { get; init; } = Array.Empty<string>();
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;
}
