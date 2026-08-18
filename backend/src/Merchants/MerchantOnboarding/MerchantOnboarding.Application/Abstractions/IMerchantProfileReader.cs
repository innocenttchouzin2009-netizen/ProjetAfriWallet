namespace AfriWallet.Merchants.Onboarding.Application.Abstractions;

public sealed record MerchantProfileSnapshot(
    string MerchantId,
    string OwnerAwid,
    string Status,
    string LegalName,
    string CountryCode,
    string SettlementCurrency);

public interface IMerchantProfileReader
{
    Task<MerchantProfileSnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default);
}
