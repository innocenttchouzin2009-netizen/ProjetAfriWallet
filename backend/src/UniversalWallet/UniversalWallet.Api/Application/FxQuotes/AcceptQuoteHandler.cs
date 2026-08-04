using UniversalWallet.Api.Infrastructure.FxQuotes;
using UniversalWallet.Api.Domain.FxQuotes;

namespace UniversalWallet.Api.Application.FxQuotes;

public sealed class AcceptQuoteHandler
{
	private readonly InMemoryQuoteRepository _repository;

	public AcceptQuoteHandler(InMemoryQuoteRepository repository)
	{
		_repository = repository;
	}

	public FxQuote Handle(Guid quoteId)
	{
		var quote = _repository.Get(quoteId);
		if (quote is null)
		{
			throw new InvalidOperationException("QUOTE_NOT_FOUND");
		}
		quote.Accept();
		_repository.Save(quote);
		return quote;
	}
}
