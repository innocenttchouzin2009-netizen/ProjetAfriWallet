using MerchantDomain = AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class MerchantRegistryService
{
    private readonly List<MerchantDomain.Merchant> _merchants = [];

    public Task<IReadOnlyList<MerchantDomain.Merchant>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MerchantDomain.Merchant>>(_merchants);

    public Task<MerchantDomain.Merchant?> GetByIdAsync(string merchantId, CancellationToken cancellationToken = default)
        => Task.FromResult(_merchants.FirstOrDefault(x => x.MerchantId == merchantId));

    public Task<MerchantDomain.Merchant> CreateAsync(MerchantDomain.Merchant merchant, CancellationToken cancellationToken = default)
    {
        Validate(merchant);
        merchant.MerchantId = string.IsNullOrWhiteSpace(merchant.MerchantId) ? Guid.NewGuid().ToString("N") : merchant.MerchantId;
        merchant.CreatedAt = merchant.CreatedAt == default ? DateTimeOffset.UtcNow : merchant.CreatedAt;
        merchant.UpdatedAt = merchant.UpdatedAt == default ? DateTimeOffset.UtcNow : merchant.UpdatedAt;
        merchant.Version = merchant.Version > 0 ? merchant.Version : 1;
        _merchants.Add(merchant);
        return Task.FromResult(merchant);
    }

    public Task<MerchantDomain.Merchant?> UpdateAsync(string merchantId, MerchantDomain.Merchant merchant, CancellationToken cancellationToken = default)
    {
        var existing = _merchants.FirstOrDefault(x => x.MerchantId == merchantId);
        if (existing is null) return Task.FromResult<MerchantDomain.Merchant?>(null);

        Validate(merchant);
        existing.BusinessName = merchant.BusinessName;
        existing.DisplayName = merchant.DisplayName;
        existing.MerchantType = merchant.MerchantType;
        existing.MerchantCategoryCode = merchant.MerchantCategoryCode;
        existing.CountryCode = merchant.CountryCode;
        existing.BaseCurrency = merchant.BaseCurrency;
        existing.SettlementCurrency = merchant.SettlementCurrency;
        existing.BusinessRegistrationNumber = merchant.BusinessRegistrationNumber;
        existing.TaxIdentifier = merchant.TaxIdentifier;
        existing.Status = merchant.Status;
        existing.Capabilities = merchant.Capabilities;
        existing.PreferredSettlementMethod = merchant.PreferredSettlementMethod;
        existing.WalletId = merchant.WalletId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.Version += 1;
        return Task.FromResult<MerchantDomain.Merchant?>(existing);
    }

    public Task<MerchantDomain.Merchant?> ActivateAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        var merchant = _merchants.FirstOrDefault(x => x.MerchantId == merchantId);
        if (merchant is null) return Task.FromResult<MerchantDomain.Merchant?>(null);
        merchant.Status = MerchantDomain.MerchantStatus.Active;
        merchant.UpdatedAt = DateTimeOffset.UtcNow;
        merchant.Version += 1;
        return Task.FromResult<MerchantDomain.Merchant?>(merchant);
    }

    public Task<MerchantDomain.Merchant?> SuspendAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        var merchant = _merchants.FirstOrDefault(x => x.MerchantId == merchantId);
        if (merchant is null) return Task.FromResult<MerchantDomain.Merchant?>(null);
        merchant.Status = MerchantDomain.MerchantStatus.Suspended;
        merchant.UpdatedAt = DateTimeOffset.UtcNow;
        merchant.Version += 1;
        return Task.FromResult<MerchantDomain.Merchant?>(merchant);
    }

    public Task<MerchantDomain.Merchant?> CloseAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        var merchant = _merchants.FirstOrDefault(x => x.MerchantId == merchantId);
        if (merchant is null) return Task.FromResult<MerchantDomain.Merchant?>(null);
        merchant.Status = MerchantDomain.MerchantStatus.Closed;
        merchant.UpdatedAt = DateTimeOffset.UtcNow;
        merchant.Version += 1;
        return Task.FromResult<MerchantDomain.Merchant?>(merchant);
    }

    public Task<bool> ExistsAsync(string merchantCode, CancellationToken cancellationToken = default)
        => Task.FromResult(_merchants.Any(x => string.Equals(x.MerchantCode, merchantCode, StringComparison.OrdinalIgnoreCase)));

    private static void Validate(MerchantDomain.Merchant merchant)
    {
        if (string.IsNullOrWhiteSpace(merchant.MerchantCode)) throw new InvalidOperationException("MerchantCode is required.");
        if (string.IsNullOrWhiteSpace(merchant.CountryCode)) throw new InvalidOperationException("CountryCode is required.");
        if (string.IsNullOrWhiteSpace(merchant.BaseCurrency)) throw new InvalidOperationException("BaseCurrency is required.");
        if (merchant.CountryCode != "CM" && merchant.CountryCode != "CI" && merchant.CountryCode != "SN") throw new InvalidOperationException("Unsupported country.");
        if (merchant.BaseCurrency != "XAF" && merchant.BaseCurrency != "USD") throw new InvalidOperationException("Unsupported currency.");
        if (!Enum.IsDefined(merchant.MerchantType)) throw new InvalidOperationException("Invalid merchant type.");
        if (!Enum.IsDefined(merchant.MerchantCategoryCode)) throw new InvalidOperationException("Invalid merchant category.");
        if (string.IsNullOrWhiteSpace(merchant.WalletId)) throw new InvalidOperationException("WalletId is required.");
    }
}
