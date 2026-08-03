namespace UniversalWallet.Api.Application.Fx;

public sealed class GetExchangeRateHandler
{
	private readonly FxEngineService _service;

	public GetExchangeRateHandler(FxEngineService service)
	{
		_service = service;
	}

	public FxRateResponse Handle(string from, string to) => _service.GetLatestRate(from, to);
}
