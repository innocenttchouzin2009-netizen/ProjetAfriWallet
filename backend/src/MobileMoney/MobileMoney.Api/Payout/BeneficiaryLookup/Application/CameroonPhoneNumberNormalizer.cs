namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application;

public static class CameroonPhoneNumberNormalizer
{
    private const string CountryCode = "237";
    private const int NationalNumberLength = 9;

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Cameroon phone number is required.", nameof(value));

        var trimmed = value.Trim();
        var compact = new string(trimmed.Where(char.IsDigit).ToArray());

        string nationalNumber;
        if (trimmed.StartsWith("+", StringComparison.Ordinal))
        {
            if (!trimmed.StartsWith("+237", StringComparison.Ordinal))
                throw new ArgumentException(
                    "Only Cameroon country code +237 is supported.",
                    nameof(value));

            nationalNumber = compact[CountryCode.Length..];
        }
        else if (compact.StartsWith(CountryCode, StringComparison.Ordinal))
        {
            nationalNumber = compact[CountryCode.Length..];
        }
        else
        {
            nationalNumber = compact;
        }

        if (nationalNumber.Length != NationalNumberLength ||
            nationalNumber.Any(ch => ch is < '0' or > '9') ||
            nationalNumber[0] != '6')
        {
            throw new ArgumentException(
                "Cameroon mobile number must contain exactly 9 national digits and start with 6.",
                nameof(value));
        }

        return $"+{CountryCode}{nationalNumber}";
    }
}
