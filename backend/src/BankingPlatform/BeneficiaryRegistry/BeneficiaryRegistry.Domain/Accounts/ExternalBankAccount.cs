namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;

public sealed class ExternalBankAccount
{
    public ExternalBankAccount(
        Guid bankAccountId,
        BankAccountIdentifier identifier,
        string bankName,
        string countryCode,
        string currencyCode,
        string accountHolderName)
    {
        if (bankAccountId == Guid.Empty)
            throw new ArgumentException("Bank account ID is required.");

        BankAccountId = bankAccountId;
        Identifier = identifier;
        BankName = Require(bankName);
        CountryCode = NormalizeCountry(countryCode);
        CurrencyCode = NormalizeCurrency(currencyCode);
        AccountHolderName = Require(accountHolderName);
    }

    public Guid BankAccountId { get; }

    public BankAccountIdentifier Identifier { get; }

    public string BankName { get; }

    public string CountryCode { get; }

    public string CurrencyCode { get; }

    public string AccountHolderName { get; }

    public BankAccountStatus Status { get; private set; } = BankAccountStatus.PendingVerification;

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public DateTime? VerifiedAtUtc { get; private set; }

    public void Verify()
    {
        if (Status == BankAccountStatus.Disabled)
            throw new InvalidOperationException("Disabled bank account cannot be verified.");

        Status = BankAccountStatus.Verified;
        VerifiedAtUtc = DateTime.UtcNow;
    }

    public void Disable()
    {
        Status = BankAccountStatus.Disabled;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }

    private static string NormalizeCountry(string value)
    {
        var result = Require(value).ToUpperInvariant();

        if (result.Length != 2)
            throw new ArgumentException("Country must use ISO alpha-2.");

        return result;
    }

    private static string NormalizeCurrency(string value)
    {
        var result = Require(value).ToUpperInvariant();

        if (result.Length != 3)
            throw new ArgumentException("Currency must use ISO 4217.");

        return result;
    }
}

public enum BankAccountStatus
{
    PendingVerification,
    Verified,
    Disabled
}
