using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Application.Ledger;

public sealed class ReverseTransactionHandler
{
	private readonly LedgerPostingService _postingService;

	public ReverseTransactionHandler(LedgerPostingService postingService)
	{
		_postingService = postingService;
	}

	public PostingResult Handle(ReverseTransactionRequest request) => _postingService.Reverse(request);
}
