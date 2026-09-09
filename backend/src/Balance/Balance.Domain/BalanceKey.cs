using AfriWallet.Ledger.Domain;

namespace AfriWallet.Balance.Domain;

public readonly record struct BalanceKey
{
    public AccountId AccountId { get; }
    public string CurrencyCode { get; }

    public BalanceKey(AccountId accountId, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ISO-like letters.", nameof(currencyCode));
        }

        AccountId = accountId;
        CurrencyCode = normalizedCurrency;
    }
}
