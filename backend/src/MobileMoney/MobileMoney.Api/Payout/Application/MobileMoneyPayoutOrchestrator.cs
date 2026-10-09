using System.Globalization;
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

        if (request.Beneficiary is null)
            throw new ArgumentException(
                "Beneficiary is required.",
                nameof(request));

        var idempotencyKey = request.IdempotencyKey.Trim();
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

        var requestFingerprint = CreateRequestFingerprint(
            request,
            beneficiary,
            corridor);

        var existing = await _store.FindByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            var replayCandidate = CreateCandidate(
                request,
                beneficiary,
                idempotencyKey,
                existing.CreatedAtUtc);

            var replay = await _store.CreateOrGetAsync(
                replayCandidate,
                requestFingerprint,
                cancellationToken);

            EnsureFingerprintMatches(replay, requestFingerprint);
            return ToResponse(replay.Payout);
        }

        var eligibility = await _eligibilityPolicy.EvaluateAsync(
            corridor,
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            throw new MobileMoneyPayoutEligibilityException(
                eligibility.FailureCode ??
                MobileMoneyPayoutEligibilityCodes.CorridorNotSupported);
        }

        var candidate = CreateCandidate(
            request,
            beneficiary,
            idempotencyKey,
            _clock.UtcNow);

        var createOrGet = await _store.CreateOrGetAsync(
            candidate,
            requestFingerprint,
            cancellationToken);

        EnsureFingerprintMatches(createOrGet, requestFingerprint);

        if (createOrGet.IsReplay)
            return ToResponse(createOrGet.Payout);

        var payout = createOrGet.Payout;

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

    private static MobileMoneyPayout CreateCandidate(
        CreateMobileMoneyPayoutRequest request,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey,
        DateTimeOffset createdAtUtc) =>
        MobileMoneyPayout.Create(
            request.SourceWalletId,
            request.AmountMinor,
            request.Currency,
            beneficiary,
            idempotencyKey,
            createdAtUtc);

    private static RequestFingerprint CreateRequestFingerprint(
        CreateMobileMoneyPayoutRequest request,
        MobileMoneyBeneficiary beneficiary,
        MobileMoneyPayoutCorridor corridor)
    {
        if (string.IsNullOrWhiteSpace(request.SourceWalletId))
            throw new ArgumentException(
                "Source wallet id is required.",
                nameof(request));

        var canonicalPayload = string.Join(
            "|",
            request.SourceWalletId.Trim(),
            corridor.SourceCountryCode,
            corridor.SourceCurrency,
            request.AmountMinor.ToString(CultureInfo.InvariantCulture),
            corridor.DestinationCurrency,
            beneficiary.Msisdn,
            beneficiary.CountryCode,
            beneficiary.OperatorCode);

        return RequestFingerprint.FromCanonicalPayload(canonicalPayload);
    }

    private static void EnsureFingerprintMatches(
        MobileMoneyPayoutCreateOrGetResult result,
        RequestFingerprint requestedFingerprint)
    {
        if (!result.Matches(requestedFingerprint))
            throw new MobileMoneyPayoutIdempotencyConflictException();
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
