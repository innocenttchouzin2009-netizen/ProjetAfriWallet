namespace Treasury.Domain.Accounts;

public sealed class TreasuryAccount
{
    public TreasuryAccount(
        Guid accountId,
        string accountCode,
        string displayName,
        string currencyCode,
        TreasuryAccountType type)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID is required.");

        if (string.IsNullOrWhiteSpace(accountCode))
            throw new ArgumentException("Account code is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");

        currencyCode = currencyCode.Trim().ToUpperInvariant();
        if (currencyCode.Length != 3)
            throw new ArgumentException("Currency code must use ISO 4217 format.");

        AccountId = accountId;
        AccountCode = accountCode.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        CurrencyCode = currencyCode;
        Type = type;
    }

    public Guid AccountId { get; }
    public string AccountCode { get; }
    public string DisplayName { get; private set; }
    public string CurrencyCode { get; }
    public TreasuryAccountType Type { get; }
    public TreasuryAccountStatus Status { get; private set; } = TreasuryAccountStatus.Active;
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public void Suspend()
    {
        if (Status == TreasuryAccountStatus.Closed)
            throw new InvalidOperationException("Closed treasury account is immutable.");

        Status = TreasuryAccountStatus.Suspended;
    }

    public void Activate()
    {
        if (Status == TreasuryAccountStatus.Closed)
            throw new InvalidOperationException("Closed treasury account cannot be activated.");

        Status = TreasuryAccountStatus.Active;
    }

    public void Close()
    {
        Status = TreasuryAccountStatus.Closed;
    }
}

public enum TreasuryAccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
    Clearing,
    Settlement,
    Reserve,
    Fees
}

public enum TreasuryAccountStatus
{
    Active,
    Suspended,
    Closed
}
