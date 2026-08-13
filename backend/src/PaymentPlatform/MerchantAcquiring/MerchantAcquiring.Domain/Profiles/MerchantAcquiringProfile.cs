namespace MerchantAcquiring.Domain.Profiles;

public sealed class MerchantAcquiringProfile
{
    private readonly HashSet<string> _currencies =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<AcquiringPaymentMethod> _methods = [];

    public MerchantAcquiringProfile(
        Guid acquiringProfileId,
        string merchantId,
        string countryCode,
        string settlementCurrency)
    {
        if (acquiringProfileId == Guid.Empty)
            throw new ArgumentException(
                "Acquiring profile ID is required.");

        AcquiringProfileId = acquiringProfileId;
        MerchantId = Require(merchantId);
        CountryCode = NormalizeCountry(countryCode);
        SettlementCurrency =
            NormalizeCurrency(settlementCurrency);

        _currencies.Add(SettlementCurrency);
    }

    public Guid AcquiringProfileId { get; }

    public string MerchantId { get; }

    public string CountryCode { get; }

    public string SettlementCurrency { get; }

    public MerchantAcquiringStatus Status { get; private set; }
        = MerchantAcquiringStatus.Pending;

    public IReadOnlyCollection<string> AcceptedCurrencies =>
        _currencies.ToArray();

    public IReadOnlyCollection<AcquiringPaymentMethod>
        AcceptedMethods =>
        _methods.ToArray();

    public decimal PercentageFee { get; private set; }

    public long FixedFeeMinor { get; private set; }

    public DateTime CreatedAtUtc { get; } =
        DateTime.UtcNow;

    public void Activate()
    {
        if (Status == MerchantAcquiringStatus.Closed)
            throw new InvalidOperationException(
                "Closed acquiring profile is immutable.");

        Status = MerchantAcquiringStatus.Active;
    }

    public void Suspend()
    {
        if (Status == MerchantAcquiringStatus.Closed)
            throw new InvalidOperationException(
                "Closed acquiring profile is immutable.");

        Status = MerchantAcquiringStatus.Suspended;
    }

    public void AddCurrency(string currencyCode)
    {
        EnsureMutable();
        _currencies.Add(
            NormalizeCurrency(currencyCode));
    }

    public void EnableMethod(
        AcquiringPaymentMethod method)
    {
        EnsureMutable();
        _methods.Add(method);
    }

    public void ConfigureFees(
        decimal percentageFee,
        long fixedFeeMinor)
    {
        EnsureMutable();

        if (percentageFee is < 0 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(percentageFee));

        if (fixedFeeMinor < 0)
            throw new ArgumentOutOfRangeException(
                nameof(fixedFeeMinor));

        PercentageFee = percentageFee;
        FixedFeeMinor = fixedFeeMinor;
    }

    public bool Supports(
        string currencyCode,
        AcquiringPaymentMethod method)
    {
        return Status == MerchantAcquiringStatus.Active &&
               _currencies.Contains(currencyCode) &&
               _methods.Contains(method);
    }

    private void EnsureMutable()
    {
        if (Status == MerchantAcquiringStatus.Closed)
            throw new InvalidOperationException(
                "Closed acquiring profile is immutable.");
    }

    private static string NormalizeCountry(string value)
    {
        var normalized =
            Require(value).ToUpperInvariant();

        if (normalized.Length != 2)
            throw new ArgumentException(
                "Country must use ISO alpha-2.");

        return normalized;
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized =
            Require(value).ToUpperInvariant();

        if (normalized.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217.");

        return normalized;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.");

        return value.Trim();
    }
}

public enum MerchantAcquiringStatus
{
    Pending,
    Active,
    Suspended,
    Closed
}

public enum AcquiringPaymentMethod
{
    Wallet,
    Card,
    Bank,
    MobileMoney,
    Qr
}
