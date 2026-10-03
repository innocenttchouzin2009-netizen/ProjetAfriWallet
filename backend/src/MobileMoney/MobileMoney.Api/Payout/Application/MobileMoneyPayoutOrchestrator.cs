using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Application;

public sealed class MobileMoneyPayoutOrchestrator
{
    private readonly IMobileMoneyPayoutStore _store;
    private readonly IMobileMoneyPayoutProvider _provider;
    private readonly IMobileMoneyPayoutClock _clock;
    private readonly IMobileMoneyPayoutEligibilityPolicy _eligibilityPolicy;

    public MobileMoneyPayoutOrchestrator(
        IMobileMoneyPayoutStore store,
        IMobileMoneyPayoutProvider provider,
        IMobileMoneyPayoutClock clock,
        IMobileMoneyPayoutEligibilityPolicy eligibilityPolicy)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _eligibilityPolicy = eligibilityPolicy ?? throw new ArgumentNullException(nameof(eligibilityPolicy));
    }

    public async Task<MobileMoneyPayoutResponse> CreateAndSubmitAsync(
        CreateMobileMoneyPayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(request));

        var idempotencyKey = request.IdempotencyKey.Trim();
        var existing = await _store.FindByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ToResponse(existing);

        if (request.Beneficiary is null)
            throw new ArgumentException(
                "Beneficiary is required.",
                nameof(request));

        var beneficiary = new MobileMoneyBeneficiary(
            request.Beneficiary.Msisdn,
            request.Beneficiary.CountryCode,
            request.Beneficiary.OperatorCode,
            request.Beneficiary.DisplayName);

        var corridor = new MobileMoneyPayoutCorridor(
            request.SourceCountryCode,
            request.SourceCurrency,
            beneficiary.CountryCode,
            request.Currency,
            beneficiary.OperatorCode);

        var eligibility = await _eligibilityPolicy.EvaluateAsync(
            corridor,
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            throw new MobileMoneyPayoutEligibilityException(
                eligibility.FailureCode ??
                MobileMoneyPayoutEligibilityCodes.CorridorNotSupported);
        }

        var payout = MobileMoneyPayout.Create(
            request.SourceWalletId,
            request.AmountMinor,
            request.Currency,
            beneficiary,
            idempotencyKey,
            _clock.UtcNow);

        await _store.SaveAsync(payout, cancellationToken);

        payout.Start(_clock.UtcNow);
        await _store.SaveAsync(payout, cancellationToken);

        var submission = new MobileMoneyPayoutSubmission(
            payout.PayoutId,
            payout.SourceWalletId,
            corridor.SourceCountryCode,
            corridor.SourceCurrency,
            payout.AmountMinor,
            payout.Currency,
            payout.Beneficiary,
            payout.IdempotencyKey);

        var providerResult = await _provider.SubmitAsync(
            submission,
            cancellationToken);

        if (providerResult.IsAccepted)
        {
            payout.MarkSubmitted(
                providerResult.ProviderReference!,
                _clock.UtcNow);
        }
        else
        {
            payout.Fail(
                providerResult.FailureCode!,
                _clock.UtcNow);
        }

        await _store.SaveAsync(payout, cancellationToken);
        return ToResponse(payout);
    }

    private static MobileMoneyPayoutResponse ToResponse(
        MobileMoneyPayout payout) =>
        new(
            payout.PayoutId,
            payout.Status,
            payout.SourceWalletId,
            payout.AmountMinor,
            payout.Currency,
            new MobileMoneyBeneficiaryResponse(
                payout.Beneficiary.Msisdn,
                payout.Beneficiary.CountryCode,
                payout.Beneficiary.OperatorCode,
                payout.Beneficiary.DisplayName),
            payout.ProviderReference,
            payout.FailureCode,
            payout.CreatedAtUtc,
            payout.UpdatedAtUtc);
}
