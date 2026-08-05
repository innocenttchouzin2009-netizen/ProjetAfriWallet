using System.Text.RegularExpressions;
using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;
using AfriWallet.Banking.Domain.Enums;

namespace AfriWallet.Banking.Application.Accounts;

public sealed class BankAccountService
{
    private static readonly Regex BicRegex = new("^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex IbanRegex = new("^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IBankAccountRepository _repository;

    public BankAccountService(IBankAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<BankAccount> CreateAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(account);
        var errors = Validate(normalized).ToList();

        if (errors.Count > 0)
        {
            return CreateAccountWithErrors(normalized, errors);
        }

        var existing = await _repository.FindByFingerprintAsync(normalized.Fingerprint, cancellationToken);
        if (existing is not null)
        {
            errors.Add(new BankAccountValidationError { Code = "DUPLICATE_ACCOUNT", Message = "Duplicate account fingerprint detected." });
            return CreateAccountWithErrors(normalized, errors);
        }

        var created = await _repository.CreateAsync(normalized, cancellationToken);
        return created;
    }

    public async Task<BankAccount> VerifyAsync(string bankAccountId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(bankAccountId, cancellationToken);
        if (existing is null)
        {
            return new BankAccount { BankAccountId = bankAccountId, ValidationErrors = [new BankAccountValidationError { Code = "NOT_FOUND", Message = "Account was not found." }] };
        }

        var verified = CreateUpdatedAccount(existing, VerificationStatus.Verified);
        return await _repository.UpdateAsync(verified, cancellationToken);
    }

    public async Task<BankAccount> ArchiveAsync(string bankAccountId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(bankAccountId, cancellationToken);
        if (existing is null)
        {
            return new BankAccount { BankAccountId = bankAccountId, ValidationErrors = [new BankAccountValidationError { Code = "NOT_FOUND", Message = "Account was not found." }] };
        }

        var archived = CreateArchivedAccount(existing);
        return await _repository.UpdateAsync(archived, cancellationToken);
    }

    public string MaskForLogging(BankAccount account)
    {
        var value = account.Iban ?? account.AccountNumber ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        return "****";
    }

    public string BuildAuditMessage(BankAccount account)
    {
        var masked = MaskForLogging(account);
        return $"bank-account audit accountId={account.BankAccountId} maskedValue={masked} status={account.Status}";
    }

    private static BankAccount Normalize(BankAccount account)
    {
        var iban = account.Iban?.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        var bic = account.Bic?.Trim().ToUpperInvariant();

        var fingerprintInput = string.Join("|", new[]
        {
            account.OwnerAwidId,
            account.CountryCode,
            account.CurrencyCode,
            iban ?? string.Empty,
            bic ?? string.Empty,
            account.BankCode ?? string.Empty,
            account.BranchCode ?? string.Empty,
            account.AccountNumber ?? string.Empty,
            account.RoutingScheme.ToString()
        }).Trim();

        return new BankAccount
        {
            BankAccountId = account.BankAccountId,
            OwnerAwidId = account.OwnerAwidId,
            BeneficiaryId = account.BeneficiaryId,
            AccountHolderName = account.AccountHolderName,
            AccountType = account.AccountType,
            CountryCode = account.CountryCode.ToUpperInvariant(),
            CurrencyCode = account.CurrencyCode.ToUpperInvariant(),
            Iban = iban,
            Bic = bic,
            BankCode = account.BankCode?.Trim(),
            BranchCode = account.BranchCode?.Trim(),
            AccountNumber = account.AccountNumber?.Trim(),
            RoutingScheme = account.RoutingScheme,
            VerificationStatus = account.VerificationStatus,
            Status = account.Status,
            Fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fingerprintInput))),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Version = account.Version,
            ValidationErrors = account.ValidationErrors
        };
    }

    private static BankAccount CreateAccountWithErrors(BankAccount account, IReadOnlyCollection<BankAccountValidationError> errors)
        => new()
        {
            BankAccountId = account.BankAccountId,
            OwnerAwidId = account.OwnerAwidId,
            BeneficiaryId = account.BeneficiaryId,
            AccountHolderName = account.AccountHolderName,
            AccountType = account.AccountType,
            CountryCode = account.CountryCode,
            CurrencyCode = account.CurrencyCode,
            Iban = account.Iban,
            Bic = account.Bic,
            BankCode = account.BankCode,
            BranchCode = account.BranchCode,
            AccountNumber = account.AccountNumber,
            RoutingScheme = account.RoutingScheme,
            VerificationStatus = account.VerificationStatus,
            Status = account.Status,
            Fingerprint = account.Fingerprint,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Version = account.Version,
            ValidationErrors = errors
        };

    private static BankAccount CreateUpdatedAccount(BankAccount account, VerificationStatus verificationStatus)
        => new()
        {
            BankAccountId = account.BankAccountId,
            OwnerAwidId = account.OwnerAwidId,
            BeneficiaryId = account.BeneficiaryId,
            AccountHolderName = account.AccountHolderName,
            AccountType = account.AccountType,
            CountryCode = account.CountryCode,
            CurrencyCode = account.CurrencyCode,
            Iban = account.Iban,
            Bic = account.Bic,
            BankCode = account.BankCode,
            BranchCode = account.BranchCode,
            AccountNumber = account.AccountNumber,
            RoutingScheme = account.RoutingScheme,
            VerificationStatus = verificationStatus,
            Status = account.Status,
            Fingerprint = account.Fingerprint,
            CreatedAt = account.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = account.Version + 1,
            ValidationErrors = account.ValidationErrors
        };

    private static BankAccount CreateArchivedAccount(BankAccount account)
        => new()
        {
            BankAccountId = account.BankAccountId,
            OwnerAwidId = account.OwnerAwidId,
            BeneficiaryId = account.BeneficiaryId,
            AccountHolderName = account.AccountHolderName,
            AccountType = account.AccountType,
            CountryCode = account.CountryCode,
            CurrencyCode = account.CurrencyCode,
            Iban = account.Iban,
            Bic = account.Bic,
            BankCode = account.BankCode,
            BranchCode = account.BranchCode,
            AccountNumber = account.AccountNumber,
            RoutingScheme = account.RoutingScheme,
            VerificationStatus = account.VerificationStatus,
            Status = BankAccountStatus.Archived,
            Fingerprint = account.Fingerprint,
            CreatedAt = account.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = account.Version + 1,
            ValidationErrors = account.ValidationErrors
        };

    private static IEnumerable<BankAccountValidationError> Validate(BankAccount account)
    {
        var errors = new List<BankAccountValidationError>();

        if (string.IsNullOrWhiteSpace(account.OwnerAwidId))
        {
            errors.Add(new BankAccountValidationError { Code = "OWNER_REQUIRED", Message = "OwnerAwidId is required." });
        }

        if (string.IsNullOrWhiteSpace(account.AccountHolderName))
        {
            errors.Add(new BankAccountValidationError { Code = "HOLDER_REQUIRED", Message = "AccountHolderName is required." });
        }

        if (account.RoutingScheme == TransferScheme.Sepa && string.IsNullOrWhiteSpace(account.Iban))
        {
            errors.Add(new BankAccountValidationError { Code = "IBAN_REQUIRED", Message = "IBAN is required for SEPA accounts." });
        }

        if (account.RoutingScheme == TransferScheme.Domestic)
        {
            if (string.IsNullOrWhiteSpace(account.BankCode) || string.IsNullOrWhiteSpace(account.BranchCode) || string.IsNullOrWhiteSpace(account.AccountNumber))
            {
                errors.Add(new BankAccountValidationError { Code = "LOCAL_FIELDS_REQUIRED", Message = "Local accounts require bank, branch and account numbers." });
            }
        }

        if (!string.IsNullOrWhiteSpace(account.Iban))
        {
            if (!IbanRegex.IsMatch(account.Iban))
            {
                errors.Add(new BankAccountValidationError { Code = "IBAN_INVALID", Message = "IBAN has an invalid structure." });
            }
            else
            {
                var country = account.Iban.Substring(0, 2);
                if (!country.Equals(account.CountryCode, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new BankAccountValidationError { Code = "COUNTRY_MISMATCH", Message = "IBAN country does not match declared country." });
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(account.Bic) && !BicRegex.IsMatch(account.Bic))
        {
            errors.Add(new BankAccountValidationError { Code = "BIC_INVALID", Message = "BIC has an invalid structure." });
        }

        return errors;
    }
}
