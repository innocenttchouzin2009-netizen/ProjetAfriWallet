namespace Treasury.Domain.Reservations;

public sealed class TreasuryReservation
{
    public TreasuryReservation(Guid reservationId, Guid accountId, string currencyCode, long amountMinor, string reference)
    {
        if (reservationId == Guid.Empty)
            throw new ArgumentException("Reservation ID is required.");

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID is required.");

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        ReservationId = reservationId;
        AccountId = accountId;
        CurrencyCode = currencyCode.ToUpperInvariant();
        AmountMinor = amountMinor;
        Reference = reference;
    }

    public Guid ReservationId { get; }
    public Guid AccountId { get; }
    public string CurrencyCode { get; }
    public long AmountMinor { get; }
    public string Reference { get; }
    public TreasuryReservationStatus Status { get; private set; } = TreasuryReservationStatus.Active;
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; private set; }

    public void Release()
    {
        if (Status != TreasuryReservationStatus.Active)
            throw new InvalidOperationException("Only active reservations can be released.");

        Status = TreasuryReservationStatus.Released;
        ReleasedAtUtc = DateTime.UtcNow;
    }

    public void Consume()
    {
        if (Status != TreasuryReservationStatus.Active)
            throw new InvalidOperationException("Only active reservations can be consumed.");

        Status = TreasuryReservationStatus.Consumed;
    }
}

public enum TreasuryReservationStatus
{
    Active,
    Released,
    Consumed
}
