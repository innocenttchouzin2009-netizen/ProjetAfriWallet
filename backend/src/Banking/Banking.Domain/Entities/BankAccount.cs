using AfriWallet.Banking.Domain.Enums;

namespace AfriWallet.Banking.Domain.Entities;

public sealed class BankAccount
{
    public string BankAccountId { get; init; } = Guid.NewGuid().ToString("N");
    public string OwnerAwidId { get; init; } = string.Empty;
    public string BeneficiaryId { get; init; } = string.Empty;
    public string AccountHolderName { get; init; } = string.Empty;
    public BankAccountType AccountType { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public string? BankCode { get; init; }
    public string? BranchCode { get; init; }
    public string? AccountNumber { get; init; }
    public TransferScheme RoutingScheme { get; init; }
    public VerificationStatus VerificationStatus { get; init; } = VerificationStatus.Unverified;
    public BankAccountStatus Status { get; init; } = BankAccountStatus.Active;
    public string Fingerprint { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;
    public IReadOnlyCollection<BankAccountValidationError> ValidationErrors { get; init; } = Array.Empty<BankAccountValidationError>();
}

public sealed class BankAccountValidationError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
