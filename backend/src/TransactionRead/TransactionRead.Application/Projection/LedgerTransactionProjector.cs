using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionRead.Application.Contracts;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionRead.Application.Projection;

public static class LedgerTransactionProjector
{
    public static TransactionReadItem? Project(
        WalletId walletId,
        AccountId walletAccountId,
        JournalEntry journal)
    {
        if (walletId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Wallet id cannot be empty.",
                nameof(walletId));
        }

        ArgumentNullException.ThrowIfNull(journal);

        long debitMinor = 0;
        long creditMinor = 0;

        checked
        {
            foreach (var line in journal.Lines)
            {
                if (line.AccountId != walletAccountId)
                {
                    continue;
                }

                switch (line.Side)
                {
                    case LedgerSide.Debit:
                        debitMinor += line.AmountMinor;
                        break;
                    case LedgerSide.Credit:
                        creditMinor += line.AmountMinor;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Ledger journal contains an unsupported side.");
                }
            }
        }

        if (debitMinor == creditMinor)
        {
            return null;
        }

        var incoming = creditMinor > debitMinor;
        var amountMinor = incoming
            ? checked(creditMinor - debitMinor)
            : checked(debitMinor - creditMinor);

        return new TransactionReadItem(
            journal.Id.Value,
            walletId,
            amountMinor,
            journal.CurrencyCode,
            incoming
                ? TransactionReadDirection.Incoming
                : TransactionReadDirection.Outgoing,
            TransactionReadStatus.Completed,
            journal.PostedAtUtc,
            journal.BusinessReference,
            null);
    }
}
