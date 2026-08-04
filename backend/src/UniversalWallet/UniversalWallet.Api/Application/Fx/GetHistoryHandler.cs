using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Application.Fx;

public sealed class GetHistoryHandler
{
	private readonly FxEngineService _service;

	public GetHistoryHandler(FxEngineService service)
	{
		_service = service;
	}

	public IReadOnlyList<FxRateHistory> Handle(string from, string to) => _service.GetHistory(from, to);
}
