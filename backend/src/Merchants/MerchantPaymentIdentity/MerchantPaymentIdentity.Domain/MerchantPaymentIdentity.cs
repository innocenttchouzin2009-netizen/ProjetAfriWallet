namespace AfriWallet.Merchants.PaymentIdentity.Domain;

public enum MerchantPaymentIdentityStatus
{
    Active = 1,
    Disabled = 2
}

public sealed class MerchantPaymentIdentity
{
    private MerchantPaymentIdentity(
        Guid identityId,
        string merchantAfWalId,
        string merchantId,
        Guid walletId,
        MerchantPaymentIdentityStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        IdentityId = identityId;
        MerchantAfWalId = merchantAfWalId;
        MerchantId = merchantId;
        WalletId = walletId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid IdentityId { get; }
    public string MerchantAfWalId { get; }
    public string MerchantId { get; }
    public Guid WalletId { get; }
    public MerchantPaymentIdentityStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantPaymentIdentity Create(
        string merchantAfWalId,
        string merchantId,
        Guid walletId,
        DateTimeOffset nowUtc) =>
        Restore(
            Guid.NewGuid(),
            merchantAfWalId,
            merchantId,
            walletId,
            MerchantPaymentIdentityStatus.Active,
            nowUtc,
            nowUtc);

    public static MerchantPaymentIdentity Restore(
        Guid identityId,
        string merchantAfWalId,
        string merchantId,
        Guid walletId,
        MerchantPaymentIdentityStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (identityId == Guid.Empty) throw new ArgumentException("Identity id is required.", nameof(identityId));
        if (walletId == Guid.Empty) throw new ArgumentException("Wallet id is required.", nameof(walletId));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (createdAtUtc.Offset != TimeSpan.Zero || updatedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Merchant payment identity timestamps must be UTC.");
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede creation.");

        var normalizedMerchantId = NormalizeMerchantId(merchantId);
        var normalizedAfWalId = NormalizeMerchantAfWalId(merchantAfWalId);

        return new MerchantPaymentIdentity(
            identityId,
            normalizedAfWalId,
            normalizedMerchantId,
            walletId,
            status,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Disable(DateTimeOffset atUtc)
    {
        if (atUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", nameof(atUtc));
        if (atUtc < UpdatedAtUtc)
            throw new ArgumentException("Timestamp cannot move backwards.", nameof(atUtc));
        if (Status == MerchantPaymentIdentityStatus.Disabled)
            return;

        Status = MerchantPaymentIdentityStatus.Disabled;
        UpdatedAtUtc = atUtc;
    }

    public static string NormalizeMerchantAfWalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Merchant AfWal ID is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 128)
            throw new ArgumentException("Merchant AfWal ID cannot exceed 128 characters.", nameof(value));
        if (normalized.Any(char.IsWhiteSpace))
            throw new ArgumentException("Merchant AfWal ID cannot contain whitespace.", nameof(value));

        return normalized;
    }

    public static string NormalizeMerchantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Merchant id is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("AFM-", StringComparison.Ordinal))
            throw new ArgumentException("Merchant id must start with AFM-.", nameof(value));

        return normalized;
    }
}
