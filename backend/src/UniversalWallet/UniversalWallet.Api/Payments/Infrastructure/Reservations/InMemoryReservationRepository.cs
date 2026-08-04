using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Domain.Reservations;

namespace UniversalWallet.Api.Payments.Infrastructure.Reservations;

public sealed class InMemoryReservationRepository : IPaymentReservationRepository
{
    private readonly Dictionary<Guid, FundsReservation> _reservations = new();
    private readonly Dictionary<Guid, Guid> _byIntent = new();

    public Task<FundsReservation?> GetByIntentAsync(Guid intentId, CancellationToken cancellationToken = default)
    {
        if (_byIntent.TryGetValue(intentId, out var reservationId))
        {
            return Task.FromResult<FundsReservation?>(_reservations[reservationId]);
        }

        return Task.FromResult<FundsReservation?>(null);
    }

    public Task<FundsReservation?> GetAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_reservations.GetValueOrDefault(reservationId));
    }

    public Task AddAsync(FundsReservation reservation, CancellationToken cancellationToken = default)
    {
        _reservations[reservation.Id] = reservation;
        _byIntent[reservation.PaymentIntentId] = reservation.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FundsReservation reservation, CancellationToken cancellationToken = default)
    {
        _reservations[reservation.Id] = reservation;
        return Task.CompletedTask;
    }
}
