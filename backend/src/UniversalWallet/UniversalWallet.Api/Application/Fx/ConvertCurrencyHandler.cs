namespace UniversalWallet.Api.Application.Fx;

public sealed class ConvertCurrencyHandler
{
	private readonly FxEngineService _service;

	public ConvertCurrencyHandler(FxEngineService service)
	{
		_service = service;
	}

	public FxConversionResponse Handle(ConvertCurrencyRequest request) => _service.Convert(request);
}
