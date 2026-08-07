namespace Accounting.Domain.Accounts;

public enum GeneralLedgerAccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
    Clearing,
    Suspension
}

public enum GeneralLedgerAccountStatus
{
    Active,
    Closed
}

public sealed class GeneralLedgerAccount
{
    public Guid AccountId { get; }
    public string AccountCode { get; }
    public string DisplayName { get; }
    public string CurrencyCode { get; }
    public GeneralLedgerAccountType Type { get; }
    public GeneralLedgerAccountStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }

    public GeneralLedgerAccount(
        Guid accountId,
        string accountCode,
        string displayName,
        string currencyCode,
        GeneralLedgerAccountType type,
        DateTime? createdAtUtc = null)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account identifier is required.", nameof(accountId));

        AccountId = accountId;
        AccountCode = RequireText(accountCode, nameof(accountCode)).ToUpperInvariant();
        DisplayName = RequireText(displayName, nameof(displayName));
        CurrencyCode = RequireText(currencyCode, nameof(currencyCode)).ToUpperInvariant();
        Type = type;
        Status = GeneralLedgerAccountStatus.Active;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status == GeneralLedgerAccountStatus.Closed)
            return;

        Status = GeneralLedgerAccountStatus.Closed;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }
}