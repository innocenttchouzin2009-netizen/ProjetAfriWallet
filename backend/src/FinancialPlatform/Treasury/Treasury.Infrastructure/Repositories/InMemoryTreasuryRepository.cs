using System.Collections.Concurrent;
using Treasury.Application.Interfaces;
using Treasury.Domain.Accounts;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;

namespace Treasury.Infrastructure.Repositories;

public sealed class InMemoryTreasuryRepository : ITreasuryRepository
{
    private readonly ConcurrentDictionary<Guid, TreasuryAccount> _accounts = new();
    private readonly ConcurrentDictionary<Guid, TreasuryTransaction> _transactions = new();
    private readonly ConcurrentDictionary<Guid, TreasuryReservation> _reservations = new();

    public Task AddAccountAsync(TreasuryAccount account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_accounts.TryAdd(account.AccountId, account))
            throw new InvalidOperationException("Treasury account already exists.");

        return Task.CompletedTask;
    }

    public Task<TreasuryAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task AddTransactionAsync(TreasuryTransaction transaction, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (transaction.Status != TreasuryTransactionStatus.Posted)
            throw new InvalidOperationException("Only posted transactions can enter the journal.");

        if (!_transactions.TryAdd(transaction.TransactionId, transaction))
            throw new InvalidOperationException("Treasury transaction already exists.");

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TreasuryEntry>> GetEntriesAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = _transactions.Values
            .SelectMany(x => x.Entries)
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.PostedAtUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<TreasuryEntry>>(entries);
    }

    public Task AddReservationAsync(TreasuryReservation reservation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_reservations.TryAdd(reservation.ReservationId, reservation))
            throw new InvalidOperationException("Treasury reservation already exists.");

        return Task.CompletedTask;
    }

    public Task<TreasuryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _reservations.TryGetValue(reservationId, out var reservation);
        return Task.FromResult(reservation);
    }

    public Task<IReadOnlyCollection<TreasuryReservation>> GetReservationsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reservations = _reservations.Values.Where(x => x.AccountId == accountId).ToArray();
        return Task.FromResult<IReadOnlyCollection<TreasuryReservation>>(reservations);
    }
}
