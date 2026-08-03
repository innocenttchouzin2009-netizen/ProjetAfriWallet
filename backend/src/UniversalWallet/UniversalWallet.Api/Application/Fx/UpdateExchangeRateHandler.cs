namespace UniversalWallet.Api.Application.Fx;

public sealed class UpdateExchangeRateHandler
{
	private readonly FxEngineService _service;

	public UpdateExchangeRateHandler(FxEngineService service)
	{
		_service = service;
	}

	public FxRateResponse Handle(UpdateExchangeRateRequest request) => _service.UpdateRate(request);
}
