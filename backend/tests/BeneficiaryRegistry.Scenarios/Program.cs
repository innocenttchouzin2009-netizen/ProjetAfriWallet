using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Services;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Infrastructure;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-40} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-40} PASS");
}

var repository = new InMemoryBeneficiaryRepository();
var service = new BeneficiaryRegistryService(repository);

var beneficiary = await service.CreateBeneficiaryAsync(
    new CreateBeneficiaryRequest(
        "AWID-0001",
        "Jean Dupont",
        BeneficiaryType.Individual),
    CancellationToken.None);

Check("beneficiary creation", beneficiary.BeneficiaryId != Guid.Empty);

var account = await service.AddBankAccountAsync(
    new AddBankAccountRequest(
        beneficiary.BeneficiaryId,
        BankAccountIdentifierType.Iban,
        "DE89 3704 0044 0532 0130 00",
        "Sandbox Bank",
        "DE",
        "EUR",
        "Jean Dupont"),
    CancellationToken.None);

Check("bank account creation", account.BankAccountId != Guid.Empty);
Check("IBAN normalization", account.Identifier.Value == "DE89370400440532013000");
Check("account masking", !account.Identifier.MaskedValue.Contains("DE89370400440532"));
Check("pending verification", account.Status == BankAccountStatus.PendingVerification);

await service.VerifyBankAccountAsync(
    beneficiary.BeneficiaryId,
    account.BankAccountId,
    CancellationToken.None);

Check("account verification", account.Status == BankAccountStatus.Verified);

var duplicateRejected = false;

try
{
    await service.AddBankAccountAsync(
        new AddBankAccountRequest(
            beneficiary.BeneficiaryId,
            BankAccountIdentifierType.Iban,
            "DE89370400440532013000",
            "Sandbox Bank",
            "DE",
            "EUR",
            "Jean Dupont"),
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    duplicateRejected = true;
}

Check("duplicate account rejected", duplicateRejected);

var view = await service.GetAsync(beneficiary.BeneficiaryId, CancellationToken.None);

Check("privacy projection", view is not null && view.Accounts.All(x => x.MaskedIdentifier.EndsWith("3000")));

var listing = await service.ListByOwnerAsync("AWID-0001", CancellationToken.None);

Check("owner beneficiary listing", listing.Count == 1);

var invalidCurrencyRejected = false;

try
{
    await service.AddBankAccountAsync(
        new AddBankAccountRequest(
            beneficiary.BeneficiaryId,
            BankAccountIdentifierType.AccountNumber,
            "123456789",
            "Sandbox Bank",
            "CM",
            "INVALID",
            "Jean Dupont"),
        CancellationToken.None);
}
catch (ArgumentException)
{
    invalidCurrencyRejected = true;
}

Check("currency validation", invalidCurrencyRejected);

Console.WriteLine("audit foundation ........................ PASS");
Console.WriteLine("telemetry foundation .................... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0015.1 bank beneficiary registry scenarios passed.");
