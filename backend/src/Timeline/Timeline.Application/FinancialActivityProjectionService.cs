using AfriWallet.Ledger.Domain;
using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Application;

public sealed class FinancialActivityProjectionService
{
    public FinancialActivity? Project(
        Guid ownerId,
        WalletId walletId,
        AccountId walletAccountId,
        JournalEntry journalEntry)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        if (walletId.Value == Guid.Empty)
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));

        ArgumentNullException.ThrowIfNull(journalEntry);

        var walletLines = journalEntry.Lines
            .Where(line => line.AccountId == walletAccountId)
            .ToArray();

        if (walletLines.Length == 0)
            return null;

        var side = walletLines[0].Side;
        if (walletLines.Any(line => line.Side != side))
            throw new InvalidOperationException(
                "A financial timeline projection cannot mix debit and credit lines for the same wallet account.");

        long amountMinor = 0;
        checked
        {
            foreach (var line in walletLines)
                amountMinor += line.AmountMinor;
        }

        var direction = side switch
        {
            LedgerSide.Debit => FinancialActivityDirection.Outgoing,
            LedgerSide.Credit => FinancialActivityDirection.Incoming,
            _ => throw new InvalidOperationException("Ledger line side is invalid.")
        };

        return FinancialActivity.Create(
            ownerId,
            walletId,
            FinancialActivitySource.Create("ledger", journalEntry.Id.Value.ToString("N")),
            FinancialActivityKind.LedgerPosting,
            direction,
            FinancialActivityState.Completed,
            Currency.Create(journalEntry.CurrencyCode),
            amountMinor,
            journalEntry.PostedAtUtc,
            journalEntry.PostedAtUtc,
            journalEntry.BusinessReference);
    }
}
