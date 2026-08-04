namespace UniversalWallet.Api.Domain.Fx;

public enum FxEventType
{
	ExchangeRateUpdated,
	ExchangeRateExpired,
	CurrencyEnabled,
	CurrencyDisabled,
	ConversionCalculated
}
