using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Application.Fx;

public sealed class RefreshExchangeRateHandler
{
	private readonly FxEngineService _service;

	public RefreshExchangeRateHandler(FxEngineService service)
	{
		_service = service;
	}

	public FxRateResponse Handle(string from, string to) => _service.RefreshRate(from, to);
}
