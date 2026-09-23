using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Merchants.Registry.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class EfMerchantRepository(MerchantRegistryDbContext dbContext) : IMerchantRepository
{
    public async Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(merchant);
        dbContext.Merchants.Add(Map(merchant));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(merchant);
        var entity = await dbContext.Merchants.SingleAsync(
            x => x.MerchantId == merchant.MerchantId.Value,
            cancellationToken);
        Copy(merchant, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Merchant?> GetAsync(MerchantId merchantId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Merchants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MerchantId == merchantId.Value, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<Merchant?> GetByOwnerAwidAsync(string awid, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAwid(awid);
        var entity = await dbContext.Merchants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerAwidNormalized == normalized, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public Task<bool> ExistsByLegalNameAsync(
        string legalName,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeLegalName(legalName);
        var normalizedCountry = NormalizeCountry(countryCode);
        return dbContext.Merchants.AsNoTracking().AnyAsync(
            x => x.LegalNameNormalized == normalizedName && x.CountryCode == normalizedCountry,
            cancellationToken);
    }

    private static MerchantRegistryEntity Map(Merchant merchant)
    {
        var entity = new MerchantRegistryEntity { MerchantId = merchant.MerchantId.Value };
        Copy(merchant, entity);
        return entity;
    }

    private static void Copy(Merchant merchant, MerchantRegistryEntity entity)
    {
        var profile = merchant.Profile;
        entity.OwnerAwid = merchant.OwnerAwid;
        entity.OwnerAwidNormalized = NormalizeAwid(merchant.OwnerAwid);
        entity.Status = (int)merchant.Status;
        entity.LegalName = profile.LegalName;
        entity.LegalNameNormalized = NormalizeLegalName(profile.LegalName);
        entity.TradingName = profile.TradingName;
        entity.MerchantType = (int)profile.MerchantType;
        entity.CountryCode = profile.CountryCode;
        entity.SettlementCurrency = profile.SettlementCurrency;
        entity.BusinessCategory = profile.BusinessCategory;
        entity.RegistrationNumber = profile.RegistrationNumber;
        entity.TaxNumber = profile.TaxNumber;
        entity.AddressLine1 = profile.Address.AddressLine1;
        entity.AddressLine2 = profile.Address.AddressLine2;
        entity.City = profile.Address.City;
        entity.PostalCode = profile.Address.PostalCode;
        entity.Email = profile.Contact.Email;
        entity.Phone = profile.Contact.Phone;
        entity.CapabilitiesCsv = string.Join(',', merchant.Capabilities.OrderBy(x => (int)x).Select(x => ((int)x).ToString()));
        entity.CreatedAtUtc = merchant.CreatedAtUtc;
        entity.UpdatedAtUtc = merchant.UpdatedAtUtc;
        entity.ClosedAtUtc = merchant.ClosedAtUtc;
    }

    private static Merchant Map(MerchantRegistryEntity entity)
    {
        var profile = new BusinessProfile(
            entity.LegalName,
            entity.TradingName,
            (MerchantType)entity.MerchantType,
            entity.CountryCode,
            entity.SettlementCurrency,
            entity.BusinessCategory,
            entity.RegistrationNumber,
            entity.TaxNumber,
            new BusinessAddress(entity.AddressLine1, entity.AddressLine2, entity.City, entity.PostalCode, entity.CountryCode),
            new MerchantContact(entity.Email, entity.Phone));

        var capabilities = string.IsNullOrWhiteSpace(entity.CapabilitiesCsv)
            ? Array.Empty<MerchantCapability>()
            : entity.CapabilitiesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => (MerchantCapability)int.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();

        return Merchant.Restore(
            new MerchantId(entity.MerchantId),
            entity.OwnerAwid,
            profile,
            (MerchantStatus)entity.Status,
            capabilities,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.ClosedAtUtc);
    }

    private static string NormalizeAwid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Owner AWID is required.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeLegalName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Legal name is required.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeCountry(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 2)
            throw new ArgumentException("Country code must be ISO-3166 alpha-2.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }
}
