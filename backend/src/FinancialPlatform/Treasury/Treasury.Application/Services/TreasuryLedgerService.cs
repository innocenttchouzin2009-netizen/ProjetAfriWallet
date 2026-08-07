using Treasury.Application.Interfaces;
using Treasury.Domain.Accounts;
using Treasury.Domain.Balances;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;

namespace Treasury.Application.Services;

public sealed class TreasuryLedgerService
{
    private readonly ITreasuryRepository _repository;

    public TreasuryLedgerService(ITreasuryRepository repository)
    {
        _repository = repository;
    }

    public async Task<TreasuryAccount> CreateAccountAsync(
        string accountCode,
        string displayName,
        string currencyCode,
        TreasuryAccountType type,
        CancellationToken cancellationToken)
    {
        var account = new TreasuryAccount(Guid.NewGuid(), accountCode, displayName, currencyCode, type);
        await _repository.AddAccountAsync(account, cancellationToken);
        return account;
    }

    public async Task<TreasuryTransaction> PostAsync(
        string reference,
        string correlationId,
        Guid debitAccountId,
        Guid creditAccountId,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        if (debitAccountId == creditAccountId)
            throw new InvalidOperationException("Debit and credit treasury accounts must differ.");

        var debitAccount = await RequireActiveAccountAsync(debitAccountId, cancellationToken);
        var creditAccount = await RequireActiveAccountAsync(creditAccountId, cancellationToken);

        var currency = currencyCode.ToUpperInvariant();

        if (!string.Equals(debitAccount.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(creditAccount.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Treasury account currency mismatch.");
        }

        var transaction = new TreasuryTransaction(Guid.NewGuid(), reference, correlationId);
        transaction.AddDebit(debitAccountId, currency, amountMinor);
        transaction.AddCredit(creditAccountId, currency, amountMinor);
        transaction.Post();

        await _repository.AddTransactionAsync(transaction, cancellationToken);

        return transaction;
    }

    public async Task<TreasuryBalance> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Treasury account not found.");

        var entries = await _repository.GetEntriesAsync(accountId, cancellationToken);

        var debit = entries.Sum(x => x.DebitMinor);
        var credit = entries.Sum(x => x.CreditMinor);

        var net = account.Type switch
        {
            TreasuryAccountType.Asset or
            TreasuryAccountType.Expense or
            TreasuryAccountType.Clearing or
            TreasuryAccountType.Settlement or
            TreasuryAccountType.Reserve => debit - credit,
            _ => credit - debit
        };

        return new TreasuryBalance(accountId, account.CurrencyCode, debit, credit, net, DateTime.UtcNow);
    }

    public async Task<TreasuryReservation> ReserveAsync(
        Guid accountId,
        long amountMinor,
        string reference,
        CancellationToken cancellationToken)
    {
        var account = await RequireActiveAccountAsync(accountId, cancellationToken);
        var balance = await GetBalanceAsync(accountId, cancellationToken);

        var reservations = await _repository.GetReservationsAsync(accountId, cancellationToken);
        var reserved = reservations.Where(x => x.Status == TreasuryReservationStatus.Active).Sum(x => x.AmountMinor);

        var available = balance.NetMinor - reserved;
        if (available < amountMinor)
            throw new InvalidOperationException("Insufficient treasury liquidity.");

        var reservation = new TreasuryReservation(Guid.NewGuid(), accountId, account.CurrencyCode, amountMinor, reference);
        await _repository.AddReservationAsync(reservation, cancellationToken);

        return reservation;
    }

    public async Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _repository.GetReservationAsync(reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Treasury reservation not found.");

        reservation.Release();
    }

    private async Task<TreasuryAccount> RequireActiveAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("Treasury account not found.");

        if (account.Status != TreasuryAccountStatus.Active)
            throw new InvalidOperationException("Treasury account is not active.");

        return account;
    }
}
