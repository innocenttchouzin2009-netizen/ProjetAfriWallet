namespace MultiTenant.Domain.Tenants;

public sealed class Tenant
{
    private readonly HashSet<string> _countries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _currencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _features = new(StringComparer.OrdinalIgnoreCase);

    public Tenant(
        Guid tenantId,
        string tenantCode,
        string legalName,
        string displayName,
        string countryCode,
        string baseCurrency)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        }

        TenantId = tenantId;
        TenantCode = NormalizeCode(tenantCode);
        LegalName = RequireText(legalName, nameof(legalName));
        DisplayName = RequireText(displayName, nameof(displayName));
        CountryCode = NormalizeCountry(countryCode);
        BaseCurrency = NormalizeCurrency(baseCurrency);

        _countries.Add(CountryCode);
        _currencies.Add(BaseCurrency);
    }

    public Guid TenantId { get; }

    public string TenantCode { get; }

    public string LegalName { get; private set; }

    public string DisplayName { get; private set; }

    public string CountryCode { get; }

    public string BaseCurrency { get; }

    public TenantStatus Status { get; private set; } = TenantStatus.Pending;

    public string? LogoUrl { get; private set; }

    public string? PrimaryColor { get; private set; }

    public int ApiRequestsPerMinute { get; private set; } = 100;

    public int MaximumUsers { get; private set; } = 100;

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public int Version { get; private set; } = 1;

    public IReadOnlyCollection<string> AllowedCountries => _countries.ToArray();

    public IReadOnlyCollection<string> AllowedCurrencies => _currencies.ToArray();

    public IReadOnlyCollection<string> AllowedLanguages => _languages.ToArray();

    public IReadOnlyCollection<string> EnabledFeatures => _features.ToArray();

    public void Activate()
    {
        EnsureNotClosed();
        Status = TenantStatus.Active;
        Touch();
    }

    public void Suspend()
    {
        EnsureNotClosed();
        Status = TenantStatus.Suspended;
        Touch();
    }

    public void Close()
    {
        Status = TenantStatus.Closed;
        Touch();
    }

    public void UpdateBranding(string? logoUrl, string? primaryColor)
    {
        EnsureMutable();

        if (!string.IsNullOrWhiteSpace(primaryColor) &&
            !System.Text.RegularExpressions.Regex.IsMatch(primaryColor, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new ArgumentException("Primary color must use the #RRGGBB format.");
        }

        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        PrimaryColor = string.IsNullOrWhiteSpace(primaryColor)
            ? null
            : primaryColor.ToUpperInvariant();

        Touch();
    }

    public void UpdateQuotas(int apiRequestsPerMinute, int maximumUsers)
    {
        EnsureMutable();

        if (apiRequestsPerMinute is < 1 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(apiRequestsPerMinute));
        }

        if (maximumUsers is < 1 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumUsers));
        }

        ApiRequestsPerMinute = apiRequestsPerMinute;
        MaximumUsers = maximumUsers;
        Touch();
    }

    public void AllowCountry(string countryCode)
    {
        EnsureMutable();
        _countries.Add(NormalizeCountry(countryCode));
        Touch();
    }

    public void AllowCurrency(string currencyCode)
    {
        EnsureMutable();
        _currencies.Add(NormalizeCurrency(currencyCode));
        Touch();
    }

    public void AllowLanguage(string languageCode)
    {
        EnsureMutable();

        var normalized = RequireText(languageCode, nameof(languageCode)).ToLowerInvariant();
        _languages.Add(normalized);
        Touch();
    }

    public void EnableFeature(string featureName)
    {
        EnsureMutable();
        _features.Add(RequireText(featureName, nameof(featureName)));
        Touch();
    }

    public void DisableFeature(string featureName)
    {
        EnsureMutable();
        _features.Remove(featureName);
        Touch();
    }

    private void EnsureMutable()
    {
        if (Status is TenantStatus.Closed)
        {
            throw new InvalidOperationException("A closed tenant is immutable.");
        }
    }

    private void EnsureNotClosed()
    {
        if (Status is TenantStatus.Closed)
        {
            throw new InvalidOperationException("A closed tenant cannot change status.");
        }
    }

    private void Touch()
    {
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeCode(string value)
    {
        var normalized = RequireText(value, nameof(value)).Trim().ToLowerInvariant();

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z0-9][a-z0-9-]{2,49}$"))
        {
            throw new ArgumentException("Tenant code must contain 3 to 50 lowercase letters, numbers or hyphens.");
        }

        return normalized;
    }

    private static string NormalizeCountry(string value)
    {
        var normalized = RequireText(value, nameof(value)).ToUpperInvariant();

        if (normalized.Length != 2)
        {
            throw new ArgumentException("Country code must use ISO 3166-1 alpha-2 format.");
        }

        return normalized;
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = RequireText(value, nameof(value)).ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException("Currency must use ISO 4217 format.");
        }

        return normalized;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        return value.Trim();
    }
}

public enum TenantStatus
{
    Pending,
    Active,
    Suspended,
    Closed
}