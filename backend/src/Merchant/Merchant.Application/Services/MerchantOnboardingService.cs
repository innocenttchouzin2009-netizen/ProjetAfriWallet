using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class MerchantOnboardingService
{
    private readonly Dictionary<string, MerchantOnboarding> _onboardings = new(StringComparer.OrdinalIgnoreCase);

    public MerchantOnboarding StartOnboarding(string merchantId, string businessName, string legalName, string businessType, string registrationNumber, string taxIdentifier)
    {
        var onboarding = new MerchantOnboarding
        {
            MerchantId = merchantId,
            Status = MerchantOnboardingStatus.Draft,
            Profile = new MerchantProfile
            {
                BusinessName = businessName,
                LegalName = legalName,
                BusinessType = businessType,
                RegistrationNumber = registrationNumber,
                TaxIdentifier = taxIdentifier
            }
        };

        onboarding.AuditEvents.Add("MERCHANT_ONBOARDING_STARTED");
        onboarding.TelemetryEvents.Add("merchant.onboarding.started");
        _onboardings[merchantId] = onboarding;
        return onboarding;
    }

    public MerchantOnboarding? CompleteProfile(string merchantId, MerchantProfile profile)
    {
        if (!_onboardings.TryGetValue(merchantId, out var onboarding)) return null;
        onboarding.Profile = profile;
        onboarding.Status = MerchantOnboardingStatus.ProfileCompleted;
        onboarding.AuditEvents.Add("MERCHANT_PROFILE_COMPLETED");
        onboarding.TelemetryEvents.Add("merchant.onboarding.profile.completed");
        return onboarding;
    }

    public List<string> ValidateRequiredFields(MerchantProfile profile)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.BusinessName)) errors.Add("BusinessName");
        if (string.IsNullOrWhiteSpace(profile.LegalName)) errors.Add("LegalName");
        if (string.IsNullOrWhiteSpace(profile.BusinessType)) errors.Add("BusinessType");
        if (string.IsNullOrWhiteSpace(profile.RegistrationNumber)) errors.Add("RegistrationNumber");
        if (string.IsNullOrWhiteSpace(profile.AddressLine)) errors.Add("AddressLine");
        if (string.IsNullOrWhiteSpace(profile.City)) errors.Add("City");
        if (string.IsNullOrWhiteSpace(profile.CountryCode)) errors.Add("CountryCode");
        if (string.IsNullOrWhiteSpace(profile.Phone)) errors.Add("Phone");
        if (string.IsNullOrWhiteSpace(profile.Email)) errors.Add("Email");
        return errors;
    }

    public MerchantKycCase? CreateKycCase(string merchantId)
    {
        if (!_onboardings.TryGetValue(merchantId, out var onboarding)) return null;
        onboarding.Status = MerchantOnboardingStatus.DocumentsSubmitted;
        var kyc = new MerchantKycCase
        {
            MerchantId = merchantId,
            CaseId = $"KYC-{merchantId}",
            Status = MerchantKycStatus.InProgress,
            Requirements =
            [
                new MerchantKycRequirement { Name = "Business registration", Description = "Business registration document", IsCompleted = true },
                new MerchantKycRequirement { Name = "Identity document", Description = "Owner identity document", IsCompleted = true },
                new MerchantKycRequirement { Name = "Proof of address", Description = "Address proof", IsCompleted = false }
            ]
        };
        onboarding.KycCase = kyc;
        onboarding.Status = MerchantOnboardingStatus.KycInProgress;
        onboarding.AuditEvents.Add("MERCHANT_KYC_SUBMITTED");
        onboarding.TelemetryEvents.Add("merchant.kyc.submitted");
        return kyc;
    }

    public MerchantOnboarding? ApproveKyc(string merchantId)
    {
        if (!_onboardings.TryGetValue(merchantId, out var onboarding)) return null;
        onboarding.KycCase!.Status = MerchantKycStatus.Approved;
        onboarding.Status = MerchantOnboardingStatus.KycApproved;
        onboarding.AuditEvents.Add("MERCHANT_KYC_APPROVED");
        onboarding.TelemetryEvents.Add("merchant.kyc.approved");
        return onboarding;
    }

    public MerchantOnboarding? RejectKyc(string merchantId)
    {
        if (!_onboardings.TryGetValue(merchantId, out var onboarding)) return null;
        onboarding.KycCase!.Status = MerchantKycStatus.Rejected;
        onboarding.Status = MerchantOnboardingStatus.KycRejected;
        onboarding.AuditEvents.Add("MERCHANT_KYC_REJECTED");
        onboarding.TelemetryEvents.Add("merchant.kyc.rejected");
        return onboarding;
    }

    public MerchantOnboarding? ActivateMerchant(string merchantId)
    {
        if (!_onboardings.TryGetValue(merchantId, out var onboarding)) return null;
        onboarding.Status = MerchantOnboardingStatus.Active;
        onboarding.AuditEvents.Add("MERCHANT_ACTIVATED");
        onboarding.TelemetryEvents.Add("merchant.onboarding.activated");
        return onboarding;
    }

    public MerchantOnboarding? GetOnboarding(string merchantId)
        => _onboardings.TryGetValue(merchantId, out var onboarding) ? onboarding : null;

    public IReadOnlyList<string> GetAuditEvents(string merchantId)
        => _onboardings.TryGetValue(merchantId, out var onboarding) ? onboarding.AuditEvents : [];

    public IReadOnlyList<string> GetTelemetryEvents(string merchantId)
        => _onboardings.TryGetValue(merchantId, out var onboarding) ? onboarding.TelemetryEvents : [];
}