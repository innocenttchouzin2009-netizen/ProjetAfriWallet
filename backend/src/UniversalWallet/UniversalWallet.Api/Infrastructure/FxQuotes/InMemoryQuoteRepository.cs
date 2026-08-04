using UniversalWallet.Api.Domain.FxQuotes;

namespace UniversalWallet.Api.Infrastructure.FxQuotes;

public sealed class InMemoryQuoteRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, FxQuote> _quotes = new();

	public void Save(FxQuote quote)
	{
		lock (_sync)
		{
			_quotes[quote.QuoteId] = quote;
		}
	}

	public FxQuote? Get(Guid id)
	{
		lock (_sync)
		{
			return _quotes.GetValueOrDefault(id);
		}
	}
}
