using AfriWallet.Merchants.Registry.Domain.Profiles;

namespace AfriWallet.Merchants.Registry.Domain.Merchants;

/// Canonical merchant identity and business profile only.
/// It never performs KYB verification, payment acceptance, capture, settlement, payout, or ledger mutation.
public sealed class Merchant
{
    private readonly List<MerchantCapability> _capabilities = new();

    public Merchant(MerchantId merchantId, string ownerAwid, BusinessProfile profile, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(ownerAwid))
            throw new ArgumentException("Owner AWID is required.", nameof(ownerAwid));
        ArgumentNullException.ThrowIfNull(profile);

        MerchantId = merchantId;
        OwnerAwid = ownerAwid.Trim();
        Profile = profile;
        Status = MerchantStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public MerchantId MerchantId { get; }
    public string OwnerAwid { get; }
    public MerchantStatus Status { get; private set; }
    public BusinessProfile Profile { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public IReadOnlyCollection<MerchantCapability> Capabilities => _capabilities.AsReadOnly();

    public void Register(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantStatus.Draft)
            throw new InvalidOperationException("Only draft merchants can be registered.");

        Status = MerchantStatus.Registered;
        UpdatedAtUtc = now;
    }

    public void BeginVerification(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantStatus.Registered)
            throw new InvalidOperationException("Only registered merchants can enter verification.");

        Status = MerchantStatus.PendingVerification;
        UpdatedAtUtc = now;
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantStatus.PendingVerification)
            throw new InvalidOperationException("Only merchants pending verification can be activated.");

        Status = MerchantStatus.Active;
        UpdatedAtUtc = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantStatus.Active)
            throw new InvalidOperationException("Only active merchants can be suspended.");

        Status = MerchantStatus.Suspended;
        UpdatedAtUtc = now;
    }

    public void Resume(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantStatus.Suspended)
            throw new InvalidOperationException("Only suspended merchants can be resumed.");

        Status = MerchantStatus.Active;
        UpdatedAtUtc = now;
    }

    public void UpdateProfile(BusinessProfile profile, DateTimeOffset now)
    {
        EnsureMutable();
        Profile = profile;
        UpdatedAtUtc = now;
    }

    public void SetCapabilities(IEnumerable<MerchantCapability> capabilities, DateTimeOffset now)
    {
        EnsureMutable();
        _capabilities.Clear();
        foreach (var capability in capabilities.Distinct())
            _capabilities.Add(capability);
        UpdatedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status == MerchantStatus.Closed)
            throw new InvalidOperationException("Merchant is already closed.");

        Status = MerchantStatus.Closed;
        ClosedAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status == MerchantStatus.Closed)
            throw new InvalidOperationException("Closed merchant is immutable.");
    }
}
