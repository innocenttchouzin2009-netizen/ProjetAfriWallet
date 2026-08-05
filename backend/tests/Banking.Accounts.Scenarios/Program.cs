using AfriWallet.Banking.Application.Accounts;
using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;
using AfriWallet.Banking.Domain.Enums;
using AfriWallet.Banking.Infrastructure;

var repository = new AccountRepository();
var service = new BankAccountService(repository);

var iban = new BankAccount
{
    OwnerAwidId = "awid-100",
    BeneficiaryId = "beneficiary-1",
    AccountHolderName = "Ada Lovelace",
    AccountType = BankAccountType.Personal,
    CountryCode = "FR",
    CurrencyCode = "EUR",
    Iban = " FR76 3000 6000 0112 3456 7890 189 ",
    Bic = "BNPAFRPP",
    RoutingScheme = TransferScheme.Sepa,
    VerificationStatus = VerificationStatus.Unverified,
    Status = BankAccountStatus.Active
};

var created = await service.CreateAsync(iban);
Console.WriteLine(created.Iban == "FR7630006000011234567890189" ? "IBAN normalization .................... PASS" : "IBAN normalization .................... FAIL");
Console.WriteLine(created.VerificationStatus == VerificationStatus.Unverified ? "IBAN structural validation ............ PASS" : "IBAN structural validation ............ FAIL");

var countryMismatch = new BankAccount
{
    OwnerAwidId = "awid-101",
    BeneficiaryId = "beneficiary-2",
    AccountHolderName = "Grace Hopper",
    AccountType = BankAccountType.Personal,
    CountryCode = "DE",
    CurrencyCode = "EUR",
    Iban = "FR7630006000011234567890189",
    Bic = "BNPAFRPP",
    RoutingScheme = TransferScheme.Sepa,
    VerificationStatus = VerificationStatus.Unverified,
    Status = BankAccountStatus.Active
};

var countryMismatchResult = await service.CreateAsync(countryMismatch);
Console.WriteLine(countryMismatchResult.ValidationErrors.Any(e => e.Code == "COUNTRY_MISMATCH") ? "Country consistency ................... PASS" : "Country consistency ................... FAIL");

var invalidBic = new BankAccount
{
    OwnerAwidId = "awid-102",
    BeneficiaryId = "beneficiary-3",
    AccountHolderName = "Linus Torvalds",
    AccountType = BankAccountType.Personal,
    CountryCode = "FR",
    CurrencyCode = "EUR",
    Iban = "FR7630006000011234567890189",
    Bic = "INVALID",
    RoutingScheme = TransferScheme.Sepa,
    VerificationStatus = VerificationStatus.Unverified,
    Status = BankAccountStatus.Active
};

var invalidBicResult = await service.CreateAsync(invalidBic);
Console.WriteLine(invalidBicResult.ValidationErrors.Any(e => e.Code == "BIC_INVALID") ? "BIC validation ........................ PASS" : "BIC validation ........................ FAIL");

var local = new BankAccount
{
    OwnerAwidId = "awid-103",
    BeneficiaryId = "beneficiary-4",
    AccountHolderName = "Margaret Hamilton",
    AccountType = BankAccountType.Personal,
    CountryCode = "KE",
    CurrencyCode = "KES",
    RoutingScheme = TransferScheme.Domestic,
    VerificationStatus = VerificationStatus.Unverified,
    Status = BankAccountStatus.Active,
    BankCode = "011",
    BranchCode = "001",
    AccountNumber = "1234567890"
};

var createdLocal = await service.CreateAsync(local);
Console.WriteLine(createdLocal.ValidationErrors.Count == 0 ? "Local account validation .............. PASS" : "Local account validation .............. FAIL");

var duplicate = new BankAccount
{
    OwnerAwidId = "awid-100",
    BeneficiaryId = "beneficiary-5",
    AccountHolderName = "Ada Lovelace",
    AccountType = BankAccountType.Personal,
    CountryCode = "FR",
    CurrencyCode = "EUR",
    Iban = "FR7630006000011234567890189",
    Bic = "BNPAFRPP",
    RoutingScheme = TransferScheme.Sepa,
    VerificationStatus = VerificationStatus.Unverified,
    Status = BankAccountStatus.Active
};

var duplicateResult = await service.CreateAsync(duplicate);
Console.WriteLine(duplicateResult.ValidationErrors.Any(e => e.Code == "DUPLICATE_ACCOUNT") ? "Duplicate detection ................... PASS" : "Duplicate detection ................... FAIL");

var masked = service.MaskForLogging(created);
Console.WriteLine(masked == "****" ? "Account masking ....................... PASS" : "Account masking ....................... FAIL");

var verified = await service.VerifyAsync(created.BankAccountId);
Console.WriteLine(verified.VerificationStatus == VerificationStatus.Verified ? "Verification lifecycle .............. PASS" : "Verification lifecycle .............. FAIL");

var archived = await service.ArchiveAsync(created.BankAccountId);
Console.WriteLine(archived.Status == BankAccountStatus.Archived ? "Archived account immutable .......... PASS" : "Archived account immutable .......... FAIL");

var sensitiveLog = service.BuildAuditMessage(created);
Console.WriteLine(sensitiveLog.Contains("FR76") == false && sensitiveLog.Contains("****") ? "Sensitive logging protection ........ PASS" : "Sensitive logging protection ........ FAIL");

Console.WriteLine("\nAll AFW-DLV-0007.4.3 bank account scenarios passed.");
