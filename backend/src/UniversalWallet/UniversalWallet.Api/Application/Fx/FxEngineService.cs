using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Domain.Fx;
using UniversalWallet.Api.Infrastructure.Fx;

namespace UniversalWallet.Api.Application.Fx;

public sealed class FxEngineService
{
	private readonly CurrencyRegistryService _currencyRegistry;
	private readonly IFxRateProvider _provider;
	private readonly FxRateCache _cache;
	private readonly IFxRateHistoryRepository _rateHistory;
	private readonly IFxConversionRepository _conversionRepository;
	private readonly object _sync = new();
	private long _rateVersion;

	public FxEngineService(
		CurrencyRegistryService currencyRegistry,
		IFxRateProvider provider,
		FxRateCache cache,
		IFxRateHistoryRepository rateHistory,
		IFxConversionRepository conversionRepository)
	{
		_currencyRegistry = currencyRegistry;
		_provider = provider;
		_cache = cache;
		_rateHistory = rateHistory;
		_conversionRepository = conversionRepository;
	}

	public IReadOnlyList<Currency> ListCurrencies() => _currencyRegistry.List();

	public Currency GetCurrency(string code) => _currencyRegistry.GetRequired(code);

	public FxRateResponse GetLatestRate(string from, string to)
	{
		return ToRateResponse(ResolveExchangeRate(from, to));
	}

	public FxConversionResponse Convert(ConvertCurrencyRequest request)
	{
		EnsureCurrencies(request.From, request.To);
		var sourceCurrency = GetCurrency(request.From);
		var targetCurrency = GetCurrency(request.To);
		var sourceAmount = CurrencyAmount.FromMinor(sourceCurrency.Code, request.AmountMinor);
		var exchangeRate = ResolveExchangeRate(sourceCurrency.Code, targetCurrency.Code);
		var conversion = Convert(sourceAmount, sourceCurrency, targetCurrency, exchangeRate);
		_conversionRepository.Save(conversion);
		return ToResponse(conversion, FxEventType.ConversionCalculated);
	}

	public FxRateResponse UpdateRate(UpdateExchangeRateRequest request)
	{
		EnsureCurrencies(request.BaseCurrency, request.QuoteCurrency);
		var now = DateTimeOffset.UtcNow;
		var rate = new ExchangeRate(Normalize(request.BaseCurrency), Normalize(request.QuoteCurrency), request.Rate, string.IsNullOrWhiteSpace(request.Provider) ? _provider.Name : request.Provider, now, now.AddHours(1), now, NextVersion());
		_cache.Set(rate);
		_rateHistory.Save(rate);
		return ToRateResponse(rate);
	}

	public IReadOnlyList<ExchangeRate> RateHistory() => _rateHistory.List();

	public IReadOnlyList<ExchangeRate> RateHistory(string baseCurrency, string quoteCurrency) => _rateHistory.List(baseCurrency, quoteCurrency);

	public IReadOnlyList<FxConversion> History() => _conversionRepository.List();

	public FxConversion? GetConversion(Guid conversionId) => _conversionRepository.Get(conversionId);

	private ExchangeRate ResolveExchangeRate(string from, string to)
	{
		EnsureCurrencies(from, to);

		if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
		{
			var identityRate = new ExchangeRate(Normalize(from), Normalize(to), 1m, "Identity", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, NextVersion());
			_rateHistory.Save(identityRate);
			return identityRate;
		}

		if (_cache.TryGet(from, to, out var cached))
		{
			return cached;
		}

		var rate = _provider.GetRate(from, to);
		_cache.Set(rate);
		_rateHistory.Save(rate);
		return rate;
	}

	private FxConversion Convert(CurrencyAmount sourceAmount, Currency sourceCurrency, Currency targetCurrency, ExchangeRate exchangeRate)
	{
		var sourceMajor = sourceAmount.ToMajor(sourceCurrency.MinorUnits);
		var targetMajor = sourceMajor * exchangeRate.Rate;
		var targetAmount = CurrencyAmount.FromMajor(targetCurrency.Code, targetMajor, targetCurrency.MinorUnits, MidpointRounding.ToEven);
		var fee = CurrencyAmount.FromMinor(sourceCurrency.Code, 0);
		var spread = CurrencyAmount.FromMinor(sourceCurrency.Code, 0);
		return new FxConversion(Guid.CreateVersion7(), sourceAmount, targetAmount, exchangeRate, fee, spread, DateTimeOffset.UtcNow, exchangeRate.Provider);
	}

	private void EnsureCurrencies(string from, string to)
	{
		_currencyRegistry.EnsureEnabled(from);
		_currencyRegistry.EnsureEnabled(to);
	}

	private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();

	private long NextVersion()
	{
		lock (_sync)
		{
			_rateVersion++;
			return _rateVersion;
		}
	}

	private static FxRateResponse ToRateResponse(ExchangeRate rate) => new()
	{
		BaseCurrency = rate.BaseCurrency,
		QuoteCurrency = rate.QuoteCurrency,
		Rate = rate.Rate,
		Provider = rate.Provider,
		ValidFrom = rate.ValidFrom,
		ValidUntil = rate.ValidUntil,
		CreatedAt = rate.CreatedAt,
		Version = rate.Version
	};

	private static FxConversionResponse ToResponse(FxConversion conversion, FxEventType eventType) => new()
	{
		ConversionId = conversion.ConversionId,
		SourceAmount = conversion.SourceAmount,
		TargetAmount = conversion.TargetAmount,
		ExchangeRate = ToRateResponse(conversion.ExchangeRate),
		Fee = conversion.Fee,
		Spread = conversion.Spread,
		Timestamp = conversion.Timestamp,
		Events = [eventType]
	};
}
