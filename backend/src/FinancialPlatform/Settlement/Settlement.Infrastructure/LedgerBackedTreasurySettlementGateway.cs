using System.Security.Cryptography;
using System.Text;
using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using Settlement.Application.Interfaces;
using Treasury.Application.Interfaces;
using Treasury.Domain.Accounts;

namespace Settlement.Infrastructure.Gateways;

public sealed class LedgerBackedTreasurySettlementGateway(
    LedgerPostingApplicationService ledgerPosting,
    LedgerBackedBalanceReadService balanceReader,
    ITreasuryRepository treasuryRepository,
    TimeProvider timeProvider)
    : ITreasurySettlementGateway
{
    public async Task<bool> HasAvailableFundsAsync(
        Guid accountId,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty) return false;
        if (amountMinor <= 0) return false;

        var snapshot = await balanceReader.ReadAsync(
            new BalanceKey(new AccountId(accountId), currencyCode),
            cancellationToken);

        return snapshot.NetMinor >= amountMinor;
    }

    public async Task PostSettlementAsync(TreasurySettlementPosting posting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(posting);
        ValidatePosting(posting);

        if (string.Equals(posting.SourceCurrency, posting.DestinationCurrency, StringComparison.OrdinalIgnoreCase))
        {
            await PostOrValidateAsync(
                posting.InstructionId,
                posting.SourceCurrency,
                $"settlement:{posting.InstructionId:D}",
                [
                    new PostLedgerLineCommand(posting.SourceAccountId, LedgerSide.Debit, posting.SourceAmountMinor, "Settlement source"),
                    new PostLedgerLineCommand(posting.DestinationAccountId, LedgerSide.Credit, posting.DestinationAmountMinor, "Settlement destination")
                ],
                cancellationToken);
            return;
        }

        var sourceClearing = await RequireClearingAccountAsync(posting.SourceCurrency, cancellationToken);
        var destinationClearing = await RequireClearingAccountAsync(posting.DestinationCurrency, cancellationToken);

        var sourceCorrelation = DeriveCorrelationId(posting.InstructionId, "source");
        var destinationCorrelation = DeriveCorrelationId(posting.InstructionId, "destination");

        await PostOrValidateAsync(
            sourceCorrelation,
            posting.SourceCurrency,
            $"settlement-fx-source:{posting.InstructionId:D}",
            [
                new PostLedgerLineCommand(posting.SourceAccountId, LedgerSide.Debit, posting.SourceAmountMinor, "FX settlement source"),
                new PostLedgerLineCommand(sourceClearing.AccountId, LedgerSide.Credit, posting.SourceAmountMinor, "FX source clearing")
            ],
            cancellationToken);

        await PostOrValidateAsync(
            destinationCorrelation,
            posting.DestinationCurrency,
            $"settlement-fx-destination:{posting.InstructionId:D}",
            [
                new PostLedgerLineCommand(destinationClearing.AccountId, LedgerSide.Debit, posting.DestinationAmountMinor, "FX destination clearing"),
                new PostLedgerLineCommand(posting.DestinationAccountId, LedgerSide.Credit, posting.DestinationAmountMinor, "FX settlement destination")
            ],
            cancellationToken);
    }

    private async Task<TreasuryAccount> RequireClearingAccountAsync(
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeCurrency(currencyCode);
        var account = await treasuryRepository.GetAccountByCodeAsync(
            $"FX-CLEARING-{normalized}",
            cancellationToken);

        if (account is null)
            throw new InvalidOperationException($"FX clearing account is not configured for {normalized}.");

        if (account.Type != TreasuryAccountType.Clearing ||
            account.Status != TreasuryAccountStatus.Active ||
            !string.Equals(account.CurrencyCode, normalized, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"FX clearing account for {normalized} is invalid or inactive.");
        }

        return account;
    }

    private async Task PostOrValidateAsync(
        Guid correlationId,
        string currencyCode,
        string businessReference,
        IReadOnlyCollection<PostLedgerLineCommand> lines,
        CancellationToken cancellationToken)
    {
        var existing = await ledgerPosting.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (existing.Succeeded && existing.Value is not null)
        {
            EnsureJournalMatches(existing.Value, currencyCode, businessReference, lines);
            return;
        }

        var result = await ledgerPosting.PostAsync(
            new PostJournalCommand(currencyCode, businessReference, correlationId, lines),
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (result.Succeeded) return;

        if (result.ErrorCode == LedgerErrorCode.DuplicateCorrelation)
        {
            var duplicate = await ledgerPosting.GetByCorrelationIdAsync(correlationId, cancellationToken);
            if (duplicate.Succeeded && duplicate.Value is not null)
            {
                EnsureJournalMatches(duplicate.Value, currencyCode, businessReference, lines);
                return;
            }
        }

        throw new InvalidOperationException(result.ErrorMessage ?? "Settlement ledger posting failed.");
    }

    private static void EnsureJournalMatches(
        JournalEntryView journal,
        string currencyCode,
        string businessReference,
        IReadOnlyCollection<PostLedgerLineCommand> expectedLines)
    {
        if (!string.Equals(journal.CurrencyCode, NormalizeCurrency(currencyCode), StringComparison.Ordinal) ||
            !string.Equals(journal.BusinessReference, businessReference, StringComparison.Ordinal) ||
            journal.Lines.Count != expectedLines.Count)
        {
            throw new InvalidOperationException("Settlement correlation id is already bound to a different ledger journal.");
        }

        var actual = journal.Lines
            .Select(x => (x.AccountId, x.Side, x.AmountMinor, x.Memo))
            .OrderBy(x => x.AccountId).ThenBy(x => x.Side).ThenBy(x => x.AmountMinor)
            .ToArray();
        var expected = expectedLines
            .Select(x => (x.AccountId, x.Side, x.AmountMinor, x.Memo))
            .OrderBy(x => x.AccountId).ThenBy(x => x.Side).ThenBy(x => x.AmountMinor)
            .ToArray();

        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("Settlement correlation id is already bound to different ledger lines.");
    }

    private static void ValidatePosting(TreasurySettlementPosting posting)
    {
        if (posting.InstructionId == Guid.Empty) throw new ArgumentException("Settlement instruction id is required.");
        if (posting.SourceAccountId == Guid.Empty || posting.DestinationAccountId == Guid.Empty)
            throw new ArgumentException("Settlement source and destination accounts are required.");
        if (posting.SourceAmountMinor <= 0 || posting.DestinationAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(posting), "Settlement amounts must be positive.");
        if (posting.AppliedRate <= 0m)
            throw new ArgumentOutOfRangeException(nameof(posting), "Applied FX rate must be positive.");

        var source = NormalizeCurrency(posting.SourceCurrency);
        var destination = NormalizeCurrency(posting.DestinationCurrency);
        if (!string.Equals(source, destination, StringComparison.Ordinal))
        {
            var expected = decimal.ToInt64(decimal.Round(
                posting.SourceAmountMinor * posting.AppliedRate,
                0,
                MidpointRounding.AwayFromZero));

            if (expected != posting.DestinationAmountMinor)
                throw new InvalidOperationException("Settlement destination amount does not match the applied FX quote.");
        }
        else if (posting.SourceAmountMinor != posting.DestinationAmountMinor)
        {
            throw new InvalidOperationException("Same-currency settlement amounts must match.");
        }
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Currency is required.");
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain exactly three letters.");
        return normalized;
    }

    private static Guid DeriveCorrelationId(Guid instructionId, string leg)
    {
        var data = Encoding.UTF8.GetBytes($"afwal-settlement-fx:{instructionId:D}:{leg}");
        var hash = SHA256.HashData(data);
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
