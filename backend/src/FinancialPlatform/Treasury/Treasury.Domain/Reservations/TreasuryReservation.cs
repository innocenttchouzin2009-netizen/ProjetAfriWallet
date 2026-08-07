namespace Treasury.Domain.Reservations;

public sealed class TreasuryReservation
{
    public TreasuryReservation(
        string reservationId,
        string accountId,
        decimal amount)
    {
        ReservationId = reservationId;
        AccountId = accountId;
        Amount = amount;
        Active = true;
    }

    public string ReservationId { get; }

    public string AccountId { get; }

    public decimal Amount { get; }

    public bool Active { get; private set; }

    public void Release()
    {
        Active = false;
    }
}
