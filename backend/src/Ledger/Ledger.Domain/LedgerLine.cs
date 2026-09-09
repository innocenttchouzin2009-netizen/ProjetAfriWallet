namespace AfriWallet.Ledger.Domain;

public sealed record LedgerLine
{
    public AccountId AccountId { get; }
    public LedgerSide Side { get; }
    public long AmountMinor { get; }
    public string? Memo { get; }

    public LedgerLine(AccountId accountId, LedgerSide side, long amountMinor, string? memo = null)
    {
        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side), "Ledger side is invalid.");
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Ledger amount must be positive.");
        }

        AccountId = accountId;
        Side = side;
        AmountMinor = amountMinor;
        Memo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim();
    }
}
