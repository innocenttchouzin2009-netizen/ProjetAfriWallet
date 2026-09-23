namespace AfriWallet.Merchants.WalletBinding.Domain;

public enum MerchantWalletBindingStatus
{
    Active = 0,
    Revoked = 1
}

public sealed class MerchantWalletBinding
{
    private MerchantWalletBinding(
        Guid bindingId,
        string merchantId,
        Guid walletId,
        string currency,
        MerchantWalletBindingStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        BindingId = bindingId;
        MerchantId = merchantId;
        WalletId = walletId;
        Currency = currency;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid BindingId { get; }
    public string MerchantId { get; }
    public Guid WalletId { get; private set; }
    public string Currency { get; }
    public MerchantWalletBindingStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MerchantWalletBinding Create(
        string merchantId,
        Guid walletId,
        string currency,
        DateTimeOffset nowUtc)
    {
        var normalizedMerchant = NormalizeMerchantId(merchantId);
        var normalizedCurrency = NormalizeCurrency(currency);
        if (walletId == Guid.Empty)
            throw new ArgumentException("Wallet id is required.", nameof(walletId));
        EnsureUtc(nowUtc);

        return new(
            Guid.NewGuid(),
            normalizedMerchant,
            walletId,
            normalizedCurrency,
            MerchantWalletBindingStatus.Active,
            nowUtc,
            nowUtc);
    }

    public static MerchantWalletBinding Restore(
        Guid bindingId,
        string merchantId,
        Guid walletId,
        string currency,
        MerchantWalletBindingStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (bindingId == Guid.Empty)
            throw new ArgumentException("Binding id is required.", nameof(bindingId));
        if (walletId == Guid.Empty)
            throw new ArgumentException("Wallet id is required.", nameof(walletId));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede creation.");

        return new(
            bindingId,
            NormalizeMerchantId(merchantId),
            walletId,
            NormalizeCurrency(currency),
            status,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Rebind(Guid walletId, DateTimeOffset nowUtc)
    {
        if (Status != MerchantWalletBindingStatus.Revoked)
            throw new InvalidOperationException("Only a revoked merchant wallet binding can be rebound.");
        if (walletId == Guid.Empty)
            throw new ArgumentException("Wallet id is required.", nameof(walletId));
        EnsureForward(nowUtc);

        WalletId = walletId;
        Status = MerchantWalletBindingStatus.Active;
        UpdatedAtUtc = nowUtc;
    }

    public void Revoke(DateTimeOffset nowUtc)
    {
        if (Status == MerchantWalletBindingStatus.Revoked)
            return;
        EnsureForward(nowUtc);
        Status = MerchantWalletBindingStatus.Revoked;
        UpdatedAtUtc = nowUtc;
    }

    private void EnsureForward(DateTimeOffset value)
    {
        EnsureUtc(value);
        if (value < UpdatedAtUtc)
            throw new ArgumentException("Binding timestamp cannot move backwards.");
    }

    private static string NormalizeMerchantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Merchant id is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("AFM-", StringComparison.Ordinal))
            throw new ArgumentException("Merchant id must start with AFM-.", nameof(value));
        return normalized;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", nameof(value));
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must contain exactly three ISO-like letters.", nameof(value));
        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.");
    }
}
