namespace Accounting.Application.Validation;

internal static class AccountingValidation
{
    public static string NormalizeCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));

        return currencyCode.Trim().ToUpperInvariant();
    }

    public static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }

    public static long RequirePositiveAmount(long amountMinor, string parameterName)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Amount must be positive.");

        return amountMinor;
    }
}