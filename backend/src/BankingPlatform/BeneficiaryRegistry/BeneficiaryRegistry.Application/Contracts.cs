using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;

namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Application;

public sealed record CreateBeneficiaryRequest(
    string OwnerAwid,
    string DisplayName,
    BeneficiaryType Type);

public sealed record AddBankAccountRequest(
    Guid BeneficiaryId,
    BankAccountIdentifierType IdentifierType,
    string IdentifierValue,
    string BankName,
    string CountryCode,
    string CurrencyCode,
    string AccountHolderName);

public sealed record BankAccountView(
    Guid BankAccountId,
    string BankName,
    string CountryCode,
    string CurrencyCode,
    string AccountHolderName,
    string MaskedIdentifier,
    string Status);

public sealed record BeneficiaryView(
    Guid BeneficiaryId,
    string OwnerAwid,
    string DisplayName,
    string Type,
    string Status,
    IReadOnlyCollection<BankAccountView> Accounts);
