namespace Treasury.Domain.Ledger;

public sealed record TreasuryEntry(
    Guid EntryId,
    Guid TransactionId,
    Guid AccountId,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    string Reference,
    DateTime PostedAtUtc)
{
    public static TreasuryEntry Debit(
        Guid transactionId,
        Guid accountId,
        string currencyCode,
        long amountMinor,
        string reference)
    {
        ValidateAmount(amountMinor);

        return new TreasuryEntry(
            Guid.NewGuid(),
            transactionId,
            accountId,
            currencyCode.ToUpperInvariant(),
            amountMinor,
            0,
            reference,
            DateTime.UtcNow);
    }

    public static TreasuryEntry Credit(
        Guid transactionId,
        Guid accountId,
        string currencyCode,
        long amountMinor,
        string reference)
    {
        ValidateAmount(amountMinor);

        return new TreasuryEntry(
            Guid.NewGuid(),
            transactionId,
            accountId,
            currencyCode.ToUpperInvariant(),
            0,
            amountMinor,
            reference,
            DateTime.UtcNow);
    }

    private static void ValidateAmount(long amountMinor)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Ledger amount must be greater than zero.");
    }
}
