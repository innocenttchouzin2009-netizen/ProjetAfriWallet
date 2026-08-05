using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

var service = new MerchantOnboardingService();

var onboarding = service.StartOnboarding("merchant-001", "Ariane Commerce", "Ariane Commerce SARL", "LIMITED", "123456789", "FR123456789");
AssertTrue(onboarding.MerchantId == "merchant-001", "merchant onboarding started");

var profile = new MerchantProfile
{
    BusinessName = "Ariane Commerce",
    LegalName = "Ariane Commerce SARL",
    BusinessType = "LIMITED",
    RegistrationNumber = "123456789",
    TaxIdentifier = "FR123456789",
    AddressLine = "10 Rue de la Paix",
    City = "Paris",
    CountryCode = "FR",
    Phone = "+33100000000",
    Email = "contact@ariane.example",
    MerchantCategoryCode = MerchantCategory.Retail,
    Description = "Boutique en ligne",
    Website = "https://ariane.example"
};

var updated = service.CompleteProfile("merchant-001", profile);
AssertTrue(updated?.Status == MerchantOnboardingStatus.ProfileCompleted, "profile completion");

var validation = service.ValidateRequiredFields(profile);
AssertTrue(validation.Count == 0, "required fields validation");

var kyc = service.CreateKycCase("merchant-001");
AssertTrue(kyc is not null, "kyc case creation");

var approved = service.ApproveKyc("merchant-001");
AssertTrue(approved?.Status == MerchantOnboardingStatus.KycApproved, "kyc approval flow");

var rejected = service.StartOnboarding("merchant-002", "Beta Commerce", "Beta Commerce SAS", "LIMITED", "987654321", "FR987654321");
var rejectionProfile = new MerchantProfile
{
    BusinessName = "Beta Commerce",
    LegalName = "Beta Commerce SAS",
    BusinessType = "LIMITED",
    RegistrationNumber = "987654321",
    TaxIdentifier = "FR987654321",
    AddressLine = "10 Rue de la Paix",
    City = "Paris",
    CountryCode = "FR",
    Phone = "+33100000001",
    Email = "beta@ariane.example",
    MerchantCategoryCode = MerchantCategory.Retail,
    Description = "Boutique en ligne",
    Website = "https://beta.example"
};
service.CompleteProfile("merchant-002", rejectionProfile);
service.CreateKycCase("merchant-002");
var rejectedOutcome = service.RejectKyc("merchant-002");
AssertTrue(rejectedOutcome?.Status == MerchantOnboardingStatus.KycRejected, "kyc rejection flow");

var activated = service.ActivateMerchant("merchant-001");
AssertTrue(activated?.Status == MerchantOnboardingStatus.Active, "merchant activation");

AssertTrue(service.GetAuditEvents("merchant-001").Count >= 4, "audit generation");
AssertTrue(service.GetTelemetryEvents("merchant-001").Count >= 4, "telemetry generation");

Console.WriteLine("merchant onboarding started ........ PASS");
Console.WriteLine("profile completion ................. PASS");
Console.WriteLine("required fields validation ......... PASS");
Console.WriteLine("kyc case creation .................. PASS");
Console.WriteLine("kyc approval flow .................. PASS");
Console.WriteLine("kyc rejection flow ................. PASS");
Console.WriteLine("merchant activation ................ PASS");
Console.WriteLine("audit generation ................... PASS");
Console.WriteLine("telemetry generation ............... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0009.2 merchant onboarding scenarios passed.");

static void AssertTrue(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{label} failed");
    }
}
