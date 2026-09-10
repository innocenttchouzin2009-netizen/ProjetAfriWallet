using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;

namespace AfriWallet.Balance.Application;

public sealed class BalanceProjectionService
{
    public IReadOnlyList<AccountBalanceSnapshot> Project(IEnumerable<JournalEntry> journalEntries)
    {
        ArgumentNullException.ThrowIfNull(journalEntries);

        var totals = new Dictionary<BalanceKey, RunningTotals>();

        checked
        {
            foreach (var journalEntry in journalEntries)
            {
                ArgumentNullException.ThrowIfNull(journalEntry);

                foreach (var line in journalEntry.Lines)
                {
                    var key = new BalanceKey(line.AccountId, journalEntry.CurrencyCode);
                    totals.TryGetValue(key, out var current);

                    current = line.Side switch
                    {
                        LedgerSide.Debit => current with { DebitMinor = current.DebitMinor + line.AmountMinor },
                        LedgerSide.Credit => current with { CreditMinor = current.CreditMinor + line.AmountMinor },
                        _ => throw new InvalidOperationException("Ledger entry contains an unsupported side.")
                    };

                    totals[key] = current;
                }
            }
        }

        return totals
            .Select(pair => new AccountBalanceSnapshot(pair.Key, pair.Value.DebitMinor, pair.Value.CreditMinor))
            .OrderBy(snapshot => snapshot.Key.CurrencyCode, StringComparer.Ordinal)
            .ThenBy(snapshot => snapshot.Key.AccountId.Value)
            .ToArray();
    }

    public AccountBalanceSnapshot Project(BalanceKey key, IEnumerable<JournalEntry> journalEntries)
    {
        ArgumentNullException.ThrowIfNull(journalEntries);

        long debitMinor = 0;
        long creditMinor = 0;

        checked
        {
            foreach (var journalEntry in journalEntries)
            {
                ArgumentNullException.ThrowIfNull(journalEntry);

                if (!string.Equals(journalEntry.CurrencyCode, key.CurrencyCode, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var line in journalEntry.Lines)
                {
                    if (line.AccountId != key.AccountId)
                    {
                        continue;
                    }

                    if (line.Side == LedgerSide.Debit)
                    {
                        debitMinor += line.AmountMinor;
                    }
                    else if (line.Side == LedgerSide.Credit)
                    {
                        creditMinor += line.AmountMinor;
                    }
                    else
                    {
                        throw new InvalidOperationException("Ledger entry contains an unsupported side.");
                    }
                }
            }
        }

        return new AccountBalanceSnapshot(key, debitMinor, creditMinor);
    }

    private readonly record struct RunningTotals(long DebitMinor, long CreditMinor);
}
