using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Contracts;
using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Application;

public sealed class MobileMoneyPayoutQuoteOrchestrator
{
    private readonly IMobileMoneyPayoutEligibilityPolicy _eligibilityPolicy;
    private readonly IMobileMoneyPayoutFxQuoteProvider _fxQuoteProvider;
    private readonly IMobileMoneyPayoutFeePolicy _feePolicy;
    private readonly IMobileMoneyPayoutClock _clock;
    private readonly IMobileMoneyPayoutQuoteStore _quoteStore;
    private readonly TimeSpan _quoteLifetime;

    public MobileMoneyPayoutQuoteOrchestrator(
        IMobileMoneyPayoutEligibilityPolicy eligibilityPolicy,
        IMobileMoneyPayoutFxQuoteProvider fxQuoteProvider,
        IMobileMoneyPayoutFeePolicy feePolicy,
        IMobileMoneyPayoutClock clock,
        IMobileMoneyPayoutQuoteStore quoteStore,
        TimeSpan quoteLifetime)
    {
        _eligibilityPolicy = eligibilityPolicy
            ?? throw new ArgumentNullException(nameof(eligibilityPolicy));
        _fxQuoteProvider = fxQuoteProvider
            ?? throw new ArgumentNullException(nameof(fxQuoteProvider));
        _feePolicy = feePolicy
            ?? throw new ArgumentNullException(nameof(feePolicy));
        _clock = clock
            ?? throw new ArgumentNullException(nameof(clock));
        _quoteStore = quoteStore
            ?? throw new ArgumentNullException(nameof(quoteStore));

        if (quoteLifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(quoteLifetime));

        _quoteLifetime = quoteLifetime;
    }

    public Task<MobileMoneyPayoutQuoteResponse> CreateAsync(
        CreateMobileMoneyPayoutQuoteRequest request,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(request, null, cancellationToken);

    public Task<MobileMoneyPayoutQuoteResponse> CreateAsync(
        CreateMobileMoneyPayoutQuoteRequest request,
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        return CreateCoreAsync(request, ownerUserId, cancellationToken);
    }

    private async Task<MobileMoneyPayoutQuoteResponse> CreateCoreAsync(
        CreateMobileMoneyPayoutQuoteRequest request,
        Guid? ownerUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.SourceAmountMinor));

        var corridor = new MobileMoneyPayoutCorridor(
            request.SourceCountryCode,
            request.SourceCurrency,
            request.DestinationCountryCode,
            request.DestinationCurrency,
            request.OperatorCode);

        var eligibility = await _eligibilityPolicy.EvaluateAsync(
            corridor,
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            throw new MobileMoneyPayoutEligibilityException(
                eligibility.FailureCode
                ?? MobileMoneyPayoutEligibilityCodes.CorridorNotSupported);
        }

        var fxQuote = await _fxQuoteProvider.GetQuoteAsync(
            new MobileMoneyPayoutFxQuoteRequest(
                corridor,
                request.SourceAmountMinor),
            cancellationToken);

        if (fxQuote is null)
            throw new MobileMoneyPayoutQuoteUnavailableException();

        var fees = await _feePolicy.CalculateAsync(
            new MobileMoneyPayoutFeeContext(
                corridor,
                request.SourceAmountMinor),
            cancellationToken);

        if (fees is null)
            throw new InvalidOperationException("Payout fee policy returned no fee collection.");

        var createdAtUtc = _clock.UtcNow;
        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Mobile Money payout quote clock must return UTC timestamps.");
        }

        var quote = MobileMoneyPayoutQuote.Create(
            corridor,
            request.SourceAmountMinor,
            fxQuote.DestinationAmountMinor,
            fxQuote.Rate,
            fees,
            createdAtUtc,
            createdAtUtc.Add(_quoteLifetime));

        if (ownerUserId is Guid owner)
            await _quoteStore.SaveOwnedAsync(owner, quote, cancellationToken);
        else
            await _quoteStore.SaveAsync(quote, cancellationToken);

        return ToResponse(quote);
    }

    private static MobileMoneyPayoutQuoteResponse ToResponse(
        MobileMoneyPayoutQuote quote) =>
        new(
            quote.QuoteId,
            quote.Corridor.SourceCurrency,
            quote.SourceAmountMinor,
            quote.Fees
                .Select(fee => new MobileMoneyPayoutQuoteFeeResponse(
                    fee.Code,
                    fee.AmountMinor,
                    fee.Currency))
                .ToArray(),
            quote.TotalFeeMinor,
            quote.TotalSourceDebitMinor,
            quote.Corridor.DestinationCurrency,
            quote.DestinationAmountMinor,
            quote.FxRate,
            quote.CreatedAtUtc,
            quote.ExpiresAtUtc);
}
