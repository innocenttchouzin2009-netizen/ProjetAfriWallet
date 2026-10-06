using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;

namespace MobileMoney.Production.Payout.Quote.Infrastructure;

public sealed class CoreFxMobileMoneyPayoutFxQuoteProvider
    : IMobileMoneyPayoutFxQuoteProvider
{
    private readonly IFxQuoteProvider _coreQuoteProvider;
    private readonly FxConversionService _conversionService;
    private readonly IMobileMoneyPayoutCurrencyMinorUnitProvider _minorUnitProvider;

    public CoreFxMobileMoneyPayoutFxQuoteProvider(
        IFxQuoteProvider coreQuoteProvider,
        FxConversionService conversionService,
        IMobileMoneyPayoutCurrencyMinorUnitProvider minorUnitProvider)
    {
        _coreQuoteProvider = coreQuoteProvider
            ?? throw new ArgumentNullException(nameof(coreQuoteProvider));
        _conversionService = conversionService
            ?? throw new ArgumentNullException(nameof(conversionService));
        _minorUnitProvider = minorUnitProvider
            ?? throw new ArgumentNullException(nameof(minorUnitProvider));
    }

    public async ValueTask<MobileMoneyPayoutFxQuote?> GetQuoteAsync(
        MobileMoneyPayoutFxQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pair = new CurrencyPair(
            CurrencyCode.Create(request.Corridor.SourceCurrency),
            CurrencyCode.Create(request.Corridor.DestinationCurrency));

        var quote = await _coreQuoteProvider.GetQuoteAsync(pair, cancellationToken);
        if (quote is null)
            return null;

        if (quote.Pair != pair)
        {
            throw new InvalidOperationException(
                $"FX provider returned '{quote.Pair}' for requested pair '{pair}'.");
        }

        var sourceMinorUnitDigits = _minorUnitProvider.GetMinorUnitDigits(
            pair.BaseCurrency.Value);
        var targetMinorUnitDigits = _minorUnitProvider.GetMinorUnitDigits(
            pair.QuoteCurrency.Value);

        var conversion = _conversionService.Convert(
            new FxConversionCommand(
                pair.BaseCurrency.Value,
                pair.QuoteCurrency.Value,
                request.SourceAmountMinor,
                sourceMinorUnitDigits,
                targetMinorUnitDigits,
                quote.Rate,
                quote.QuotedAtUtc));

        if (conversion.TargetAmountMinor <= 0)
        {
            throw new InvalidOperationException(
                "FX conversion produced a non-positive destination amount.");
        }

        return new MobileMoneyPayoutFxQuote(
            quote.Rate,
            conversion.TargetAmountMinor);
    }
}
