using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Application.Ledger;

public sealed class PostTransactionHandler
{
	private readonly LedgerPostingService _postingService;

	public PostTransactionHandler(LedgerPostingService postingService)
	{
		_postingService = postingService;
	}

	public PostingResult Handle(PostTransactionRequest request) => _postingService.Post(request);
}
