using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Domain;

namespace AfriWallet.Transfer.Application;

public sealed class InternalTransferPlanningService
{
    public InternalTransferPlan Prepare(PrepareInternalTransferCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Source);
        ArgumentNullException.ThrowIfNull(command.Target);

        if (!command.Source.IsActive)
        {
            throw new InvalidOperationException("Source wallet must be active.");
        }

        if (!command.Target.IsActive)
        {
            throw new InvalidOperationException("Target wallet must be active.");
        }

        if (command.Source.AccountId == command.Target.AccountId)
        {
            throw new InvalidOperationException("Source and target ledger accounts must be different.");
        }

        var sourceCurrency = NormalizeCurrency(command.Source.CurrencyCode);
        var targetCurrency = NormalizeCurrency(command.Target.CurrencyCode);
        if (!string.Equals(sourceCurrency, targetCurrency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Commit 1 supports same-currency internal transfers only; FX orchestration is not applied here.");
        }

        if (command.AmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.AmountMinor), "Transfer amount must be positive.");
        }

        if (command.Source.AvailableMinor < command.AmountMinor)
        {
            throw new InvalidOperationException("Source wallet has insufficient available funds.");
        }

        var transferId = TransferId.New();
        var intent = TransferIntent.Create(
            transferId,
            command.Source.WalletId,
            command.Target.WalletId,
            sourceCurrency,
            command.AmountMinor,
            command.CorrelationId,
            command.RequestedAtUtc);

        var journal = JournalEntry.Create(
            JournalEntryId.New(),
            intent.CurrencyCode,
            $"TRF-{intent.Id.Value:N}",
            intent.CorrelationId,
            intent.CreatedAtUtc,
            [
                new LedgerLine(command.Source.AccountId, LedgerSide.Debit, intent.AmountMinor, "Internal transfer debit"),
                new LedgerLine(command.Target.AccountId, LedgerSide.Credit, intent.AmountMinor, "Internal transfer credit")
            ]);

        return new InternalTransferPlan(intent, journal);
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ISO-like letters.", nameof(currencyCode));
        }

        return normalized;
    }
}
