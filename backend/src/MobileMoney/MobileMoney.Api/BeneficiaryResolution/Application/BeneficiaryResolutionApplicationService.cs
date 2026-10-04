using MobileMoney.Production.BeneficiaryResolution.Application.Abstractions;
using MobileMoney.Production.BeneficiaryResolution.Contracts;
using MobileMoney.Production.BeneficiaryResolution.Domain;

namespace MobileMoney.Production.BeneficiaryResolution.Application;

public sealed class BeneficiaryResolutionApplicationService
{
    private readonly IBeneficiaryCountryResolver _countryResolver;
    private readonly IBeneficiaryOperatorResolver _operatorResolver;
    private readonly IBeneficiaryAccountHolderNameResolver _accountHolderNameResolver;
    private readonly TimeProvider _timeProvider;

    public BeneficiaryResolutionApplicationService(
        IBeneficiaryCountryResolver countryResolver,
        IBeneficiaryOperatorResolver operatorResolver,
        IBeneficiaryAccountHolderNameResolver accountHolderNameResolver,
        TimeProvider? timeProvider = null)
    {
        _countryResolver = countryResolver;
        _operatorResolver = operatorResolver;
        _accountHolderNameResolver = accountHolderNameResolver;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<BeneficiaryResolutionCandidate> ResolveAsync(
        ResolveBeneficiaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var country = await _countryResolver.ResolveAsync(
            normalizedPhoneNumber,
            NormalizeOptionalCode(request.CountryCode),
            cancellationToken);

        if (country is null || string.IsNullOrWhiteSpace(country.CountryCode))
        {
            throw new BeneficiaryResolutionException(
                BeneficiaryResolutionErrorCode.UnsupportedCountry,
                "The beneficiary country could not be resolved.");
        }

        var operatorResolution = await _operatorResolver.ResolveAsync(
            normalizedPhoneNumber,
            NormalizeRequiredCode(country.CountryCode),
            NormalizeOptionalCode(request.OperatorCode),
            cancellationToken);

        if (operatorResolution is null || string.IsNullOrWhiteSpace(operatorResolution.OperatorCode))
        {
            return new BeneficiaryResolutionCandidate(
                ResolutionId: CreateResolutionId(),
                NormalizedPhoneNumber: normalizedPhoneNumber,
                CountryCode: NormalizeRequiredCode(country.CountryCode),
                OperatorCode: null,
                AccountHolderName: null,
                Status: BeneficiaryResolutionStatus.ManualEntryRequired,
                Source: country.Source,
                RequiresOperatorConfirmation: false,
                ManualEntryAllowed: true,
                ResolvedAtUtc: _timeProvider.GetUtcNow());
        }

        var countryCode = NormalizeRequiredCode(country.CountryCode);
        var operatorCode = NormalizeRequiredCode(operatorResolution.OperatorCode);

        var nameResolution = await _accountHolderNameResolver.ResolveAsync(
            normalizedPhoneNumber,
            countryCode,
            operatorCode,
            cancellationToken);

        var requiresOperatorConfirmation = operatorResolution.RequiresConfirmation;
        var status = requiresOperatorConfirmation
            ? BeneficiaryResolutionStatus.OperatorConfirmationRequired
            : BeneficiaryResolutionStatus.Resolved;

        var source = nameResolution?.Source ?? operatorResolution.Source;

        return new BeneficiaryResolutionCandidate(
            ResolutionId: CreateResolutionId(),
            NormalizedPhoneNumber: normalizedPhoneNumber,
            CountryCode: countryCode,
            OperatorCode: operatorCode,
            AccountHolderName: NormalizeOptionalName(nameResolution?.AccountHolderName),
            Status: status,
            Source: source,
            RequiresOperatorConfirmation: requiresOperatorConfirmation,
            ManualEntryAllowed: true,
            ResolvedAtUtc: _timeProvider.GetUtcNow());
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw InvalidPhoneNumber();
        }

        var trimmed = phoneNumber.Trim();
        if (!trimmed.StartsWith('+'))
        {
            throw InvalidPhoneNumber();
        }

        var digits = new string(trimmed.Skip(1).Where(char.IsDigit).ToArray());
        if (digits.Length is < 8 or > 15)
        {
            throw InvalidPhoneNumber();
        }

        var nonFormattingCharacters = trimmed
            .Skip(1)
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')')
            .ToArray();

        if (nonFormattingCharacters.Any(character => !char.IsDigit(character)))
        {
            throw InvalidPhoneNumber();
        }

        return $"+{digits}";
    }

    private static string? NormalizeOptionalCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string NormalizeRequiredCode(string value)
        => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptionalName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateResolutionId()
        => Guid.NewGuid().ToString("N");

    private static BeneficiaryResolutionException InvalidPhoneNumber()
        => new(
            BeneficiaryResolutionErrorCode.InvalidPhoneNumber,
            "The beneficiary phone number must be a valid international number.");
}
