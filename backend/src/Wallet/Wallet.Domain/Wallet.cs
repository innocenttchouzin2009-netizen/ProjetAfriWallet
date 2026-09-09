namespace AfriWallet.Wallet.Domain;

public sealed class Wallet
{
    private Wallet(
        WalletId id,
        Guid ownerId,
        Currency currency,
        CountryCode? countryCode,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Currency = currency;
        CountryCode = countryCode;
        Status = WalletStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public WalletId Id { get; }
    public Guid OwnerId { get; }
    public Currency Currency { get; }
    public CountryCode? CountryCode { get; }
    public WalletStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Wallet Create(
        WalletId id,
        Guid ownerId,
        Currency currency,
        CountryCode? countryCode,
        DateTimeOffset createdAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Wallet id cannot be empty.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        }

        ArgumentNullException.ThrowIfNull(currency);
        return new Wallet(id, ownerId, currency, countryCode, createdAtUtc);
    }

    public void Suspend(DateTimeOffset changedAtUtc)
    {
        EnsureNotClosed();
        if (Status != WalletStatus.Active)
        {
            throw new InvalidOperationException("Only an active wallet can be suspended.");
        }

        TransitionTo(WalletStatus.Suspended, changedAtUtc);
    }

    public void Activate(DateTimeOffset changedAtUtc)
    {
        EnsureNotClosed();
        if (Status != WalletStatus.Suspended)
        {
            throw new InvalidOperationException("Only a suspended wallet can be activated.");
        }

        TransitionTo(WalletStatus.Active, changedAtUtc);
    }

    public void Close(DateTimeOffset changedAtUtc)
    {
        EnsureNotClosed();
        TransitionTo(WalletStatus.Closed, changedAtUtc);
    }

    private void TransitionTo(WalletStatus target, DateTimeOffset changedAtUtc)
    {
        if (changedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(changedAtUtc), "Wallet lifecycle timestamps cannot move backwards.");
        }

        Status = target;
        UpdatedAtUtc = changedAtUtc;
    }

    private void EnsureNotClosed()
    {
        if (Status == WalletStatus.Closed)
        {
            throw new InvalidOperationException("A closed wallet is terminal and cannot transition again.");
        }
    }
}
