using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class MerchantOnboardingValidator
{
    public IReadOnlyList<string> Validate(MerchantProfile profile)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.BusinessName)) errors.Add("BusinessName is required.");
        if (string.IsNullOrWhiteSpace(profile.LegalName)) errors.Add("LegalName is required.");
        if (string.IsNullOrWhiteSpace(profile.BusinessType)) errors.Add("BusinessType is required.");
        if (string.IsNullOrWhiteSpace(profile.RegistrationNumber)) errors.Add("RegistrationNumber is required.");
        if (string.IsNullOrWhiteSpace(profile.AddressLine)) errors.Add("AddressLine is required.");
        if (string.IsNullOrWhiteSpace(profile.City)) errors.Add("City is required.");
        if (string.IsNullOrWhiteSpace(profile.CountryCode)) errors.Add("CountryCode is required.");
        if (string.IsNullOrWhiteSpace(profile.Phone)) errors.Add("Phone is required.");
        if (string.IsNullOrWhiteSpace(profile.Email)) errors.Add("Email is required.");
        return errors;
    }
}
