namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;

public sealed record BankAccountIdentifier
{
    public BankAccountIdentifier(
        BankAccountIdentifierType type,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Bank account identifier is required.");

        Type = type;
        Value = Normalize(type, value);
    }

    public BankAccountIdentifierType Type { get; }

    public string Value { get; }

    public string MaskedValue => Mask(Value);

    private static string Normalize(
        BankAccountIdentifierType type,
        string value)
    {
        var normalized =
            value
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Trim()
                .ToUpperInvariant();

        return type switch
        {
            BankAccountIdentifierType.Iban => ValidateIban(normalized),
            BankAccountIdentifierType.AccountNumber => ValidateGenericAccount(normalized),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private static string ValidateIban(string value)
    {
        if (value.Length is < 15 or > 34)
            throw new ArgumentException("IBAN length is invalid.");

        if (!value.All(char.IsLetterOrDigit))
            throw new ArgumentException("IBAN contains invalid characters.");

        return value;
    }

    private static string ValidateGenericAccount(string value)
    {
        if (value.Length is < 4 or > 40)
            throw new ArgumentException("Account number length is invalid.");

        if (!value.All(char.IsLetterOrDigit))
            throw new ArgumentException("Account number contains invalid characters.");

        return value;
    }

    private static string Mask(string value)
    {
        if (value.Length <= 4)
            return new string('*', value.Length);

        return $"{new string('*', value.Length - 4)}{value[^4..]}";
    }
}

public enum BankAccountIdentifierType
{
    Iban,
    AccountNumber
}
