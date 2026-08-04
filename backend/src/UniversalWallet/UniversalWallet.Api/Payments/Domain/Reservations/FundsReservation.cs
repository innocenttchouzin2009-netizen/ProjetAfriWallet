namespace UniversalWallet.Api.Payments.Domain.Reservations;

public enum FundsReservationStatus
{
    Active,
    Released,
    Consumed,
    Expired
}

public sealed class FundsReservation
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PaymentIntentId { get; init; }
    public Guid WalletId { get; init; }
    public long AmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public FundsReservationStatus Status { get; set; } = FundsReservationStatus.Active;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int Version { get; set; } = 1;

    public bool CanConsume => Status == FundsReservationStatus.Active;

    public void Release()
    {
        if (Status != FundsReservationStatus.Active)
        {
            throw new InvalidOperationException("RESERVATION_NOT_ACTIVE");
        }

        Status = FundsReservationStatus.Released;
        ReleasedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void Consume()
    {
        if (Status != FundsReservationStatus.Active)
        {
            throw new InvalidOperationException("RESERVATION_NOT_ACTIVE");
        }

        Status = FundsReservationStatus.Consumed;
        ConsumedAt = DateTimeOffset.UtcNow;
        Version += 1;
    }

    public void Expire()
    {
        if (Status != FundsReservationStatus.Active)
        {
            return;
        }

        Status = FundsReservationStatus.Expired;
        Version += 1;
    }
}
