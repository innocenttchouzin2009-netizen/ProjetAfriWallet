using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Domain.Fx;
using UniversalWallet.Api.Infrastructure.Fx;

namespace UniversalWallet.Api.Application.Fx;

public sealed class FxEngineService
{
	private readonly CurrencyRegistryService _currencyRegistry;
	private readonly IFxRateProvider _provider;
	private readonly FxRateCache _cache;
	private readonly InMemoryFxRateRepository _rateRepository;
	private readonly InMemoryFxHistoryRepository _historyRepository;
	private readonly IFxConversionRepository _conversionRepository;
	private readonly object _sync = new();
	private long _rateVersion;

	public FxEngineService(
		CurrencyRegistryService currencyRegistry,
		IFxRateProvider provider,
		FxRateCache cache,
		InMemoryFxRateRepository rateRepository,
		InMemoryFxHistoryRepository historyRepository,
		IFxConversionRepository conversionRepository)
	{
		_currencyRegistry = currencyRegistry;
		_provider = provider;
		_cache = cache;
		_rateRepository = rateRepository;
		_historyRepository = historyRepository;
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
		var rate = new ExchangeRate(
			Guid.CreateVersion7(),
			Normalize(request.BaseCurrency),
			Normalize(request.QuoteCurrency),
			request.Rate,
			string.IsNullOrWhiteSpace(request.Provider) ? _provider.Name : request.Provider,
			null,
			now,
			now.AddHours(1),
			ExchangeRateStatus.Active,
			NextVersion(),
			now);
		_cache.Set(rate);
		_rateRepository.Save(rate);
		_historyRepository.Save(new FxRateHistory(rate.Id, rate.BaseCurrency, rate.QuoteCurrency, rate.Rate, rate.Provider, now));
		return ToRateResponse(rate);
	}

	public FxRateResponse RefreshRate(string from, string to)
	{
		var rate = ResolveExchangeRate(from, to);
		return ToRateResponse(rate);
	}

	public IReadOnlyList<ExchangeRate> RateHistory() => _rateRepository.List();

	public IReadOnlyList<ExchangeRate> RateHistory(string baseCurrency, string quoteCurrency) => _rateRepository.List(baseCurrency, quoteCurrency);

	public IReadOnlyList<FxRateHistory> GetHistory(string baseCurrency, string quoteCurrency) => _historyRepository.List(baseCurrency, quoteCurrency);

	public IReadOnlyList<FxConversion> History() => _conversionRepository.List();

	public FxConversion? GetConversion(Guid conversionId) => _conversionRepository.Get(conversionId);

	private ExchangeRate ResolveExchangeRate(string from, string to)
	{
		EnsureCurrencies(from, to);
		var normalizedFrom = Normalize(from);
		var normalizedTo = Normalize(to);

		if (string.Equals(normalizedFrom, normalizedTo, StringComparison.OrdinalIgnoreCase))
		{
			var identityRate = new ExchangeRate(
				Guid.CreateVersion7(),
				normalizedFrom,
				normalizedTo,
				1m,
				"Identity",
				null,
				DateTimeOffset.UtcNow,
				DateTimeOffset.UtcNow.AddHours(1),
				ExchangeRateStatus.Active,
				NextVersion(),
				DateTimeOffset.UtcNow);
			_rateRepository.Save(identityRate);
			_historyRepository.Save(new FxRateHistory(identityRate.Id, identityRate.BaseCurrency, identityRate.QuoteCurrency, identityRate.Rate, identityRate.Provider, identityRate.CreatedAt));
			return identityRate;
		}

		if (_cache.TryGet(normalizedFrom, normalizedTo, out var cached))
		{
			return cached;
		}

		try
		{
			var rate = _provider.GetRateAsync(normalizedFrom, normalizedTo, CancellationToken.None).GetAwaiter().GetResult();
			ValidateRatePeriod(rate);
			_cache.Set(rate);
			_rateRepository.Save(rate);
			_historyRepository.Save(new FxRateHistory(rate.Id, rate.BaseCurrency, rate.QuoteCurrency, rate.Rate, rate.Provider, DateTimeOffset.UtcNow));
			return rate;
		}
		catch (InvalidOperationException ex) when (ex.Message == "FX_RATE_NOT_FOUND")
		{
			var reciprocal = _provider.GetRateAsync(normalizedTo, normalizedFrom, CancellationToken.None).GetAwaiter().GetResult();
			var derivedRate = new ExchangeRate(
				Guid.CreateVersion7(),
				normalizedFrom,
				normalizedTo,
				1m / reciprocal.Rate,
				reciprocal.Provider,
				null,
				reciprocal.ValidFrom,
				reciprocal.ValidUntil,
				ExchangeRateStatus.Active,
				NextVersion(),
				DateTimeOffset.UtcNow);
			if (derivedRate.ValidUntil <= derivedRate.ValidFrom)
			{
				throw new InvalidOperationException("FX_RATE_PERIOD_INVALID");
			}
			_cache.Set(derivedRate);
			_rateRepository.Save(derivedRate);
			_historyRepository.Save(new FxRateHistory(derivedRate.Id, derivedRate.BaseCurrency, derivedRate.QuoteCurrency, derivedRate.Rate, derivedRate.Provider, DateTimeOffset.UtcNow));
			return derivedRate;
		}
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

	private static void ValidateRatePeriod(ExchangeRate rate)
	{
		if (rate.ValidUntil <= rate.ValidFrom)
		{
			throw new InvalidOperationException("FX_RATE_PERIOD_INVALID");
		}
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
