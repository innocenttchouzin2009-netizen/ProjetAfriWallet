using Treasury.Domain.Accounts;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;

namespace Treasury.Application.Interfaces;

public interface ITreasuryRepository
{
    Task AddAccountAsync(TreasuryAccount account, CancellationToken cancellationToken);
    Task<TreasuryAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddTransactionAsync(TreasuryTransaction transaction, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TreasuryEntry>> GetEntriesAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddReservationAsync(TreasuryReservation reservation, CancellationToken cancellationToken);
    Task<TreasuryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TreasuryReservation>> GetReservationsAsync(Guid accountId, CancellationToken cancellationToken);
}
