using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Domain.Fx;
using UniversalWallet.Api.Domain.FxQuotes;
using UniversalWallet.Api.Infrastructure.Fx;
using UniversalWallet.Api.Infrastructure.FxQuotes;

namespace UniversalWallet.Api.Application.FxQuotes;

public sealed class CreateQuoteHandler
{
	private readonly FxEngineService _fxEngine;
	private readonly QuoteCalculator _calculator;
	private readonly InMemoryQuoteRepository _repository;
	private readonly CurrencyRegistryService _currencyRegistry;

	public CreateQuoteHandler(FxEngineService fxEngine, QuoteCalculator calculator, InMemoryQuoteRepository repository, CurrencyRegistryService currencyRegistry)
	{
		_fxEngine = fxEngine;
		_calculator = calculator;
		_repository = repository;
		_currencyRegistry = currencyRegistry;
	}

	public FxQuote Handle(CreateQuoteRequest request)
	{
		_currencyRegistry.EnsureEnabled(request.From);
		_currencyRegistry.EnsureEnabled(request.To);
		var rate = _fxEngine.GetLatestRate(request.From, request.To);
		var effectiveRate = new ExchangeRate(
			Guid.CreateVersion7(),
			rate.BaseCurrency,
			rate.QuoteCurrency,
			rate.Rate,
			rate.Provider,
			null,
			rate.ValidFrom,
			rate.ValidUntil,
			ExchangeRateStatus.Active,
			rate.Version,
			rate.CreatedAt);
		var quote = _calculator.CreateQuote(request.From, request.To, request.AmountMinor, effectiveRate, request.SpreadPercentage, request.TrustScore, request.Provider);
		_repository.Save(quote);
		return quote;
	}
}

public sealed class CreateQuoteRequest
{
	public string From { get; init; } = string.Empty;
	public string To { get; init; } = string.Empty;
	public long AmountMinor { get; init; }
	public decimal SpreadPercentage { get; init; } = 0.0035m;
	public decimal TrustScore { get; init; } = 0.999m;
	public string Provider { get; init; } = "Default";
}
