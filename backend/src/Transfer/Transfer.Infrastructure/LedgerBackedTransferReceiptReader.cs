using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class LedgerBackedTransferReceiptReader : ITransferReceiptReader
{
    private const string TransferReferencePrefix = "TRF-";

    private readonly IJournalRepository journalRepository;
    private readonly IReadOnlyDictionary<AccountId, Guid> walletByAccount;

    public LedgerBackedTransferReceiptReader(
        IJournalRepository journalRepository,
        IReadOnlyDictionary<Guid, AccountId> walletLedgerAccountMappings)
    {
        ArgumentNullException.ThrowIfNull(journalRepository);
        ArgumentNullException.ThrowIfNull(walletLedgerAccountMappings);

        this.journalRepository = journalRepository;
        walletByAccount = BuildReverseMappings(walletLedgerAccountMappings);
    }

    public async Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));

        cancellationToken.ThrowIfCancellationRequested();

        var journal = await journalRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        if (journal is null)
            return null;

        if (journal.CorrelationId != correlationId)
            throw new InvalidOperationException("Ledger journal correlation id does not match the requested correlation id.");

        var transferId = ParseTransferId(journal.BusinessReference);
        var debit = SingleLine(journal, LedgerSide.Debit);
        var credit = SingleLine(journal, LedgerSide.Credit);

        if (debit.AmountMinor != credit.AmountMinor)
            throw new InvalidOperationException("Transfer journal debit and credit amounts must match.");

        var sourceWalletId = ResolveWalletId(debit.AccountId, "source");
        var targetWalletId = ResolveWalletId(credit.AccountId, "target");

        return TransferReceiptReadModel.Create(
            transferId,
            journal.Id,
            sourceWalletId,
            targetWalletId,
            journal.CurrencyCode,
            debit.AmountMinor,
            journal.CorrelationId,
            journal.PostedAtUtc);
    }

    private Guid ResolveWalletId(AccountId accountId, string role) =>
        walletByAccount.TryGetValue(accountId, out var walletId)
            ? walletId
            : throw new InvalidOperationException($"Transfer {role} ledger account is not mapped to a wallet.");

    private static LedgerLine SingleLine(JournalEntry journal, LedgerSide side)
    {
        var lines = journal.Lines.Where(line => line.Side == side).ToArray();
        if (lines.Length != 1)
            throw new InvalidOperationException($"Transfer journal must contain exactly one {side} line.");

        return lines[0];
    }

    private static TransferId ParseTransferId(string businessReference)
    {
        if (string.IsNullOrWhiteSpace(businessReference) ||
            !businessReference.StartsWith(TransferReferencePrefix, StringComparison.Ordinal) ||
            !Guid.TryParse(businessReference[TransferReferencePrefix.Length..], out var transferId) ||
            transferId == Guid.Empty)
        {
            throw new InvalidOperationException("Ledger journal is not a valid internal transfer receipt.");
        }

        return new TransferId(transferId);
    }

    private static IReadOnlyDictionary<AccountId, Guid> BuildReverseMappings(
        IReadOnlyDictionary<Guid, AccountId> mappings)
    {
        var reverse = new Dictionary<AccountId, Guid>();
        foreach (var mapping in mappings)
        {
            if (mapping.Key == Guid.Empty)
                throw new ArgumentException("Wallet id mapping cannot be empty.", nameof(mappings));

            if (!reverse.TryAdd(mapping.Value, mapping.Key))
                throw new InvalidOperationException("Each ledger account can map to only one wallet for receipt reconstruction.");
        }

        return reverse;
    }
}
