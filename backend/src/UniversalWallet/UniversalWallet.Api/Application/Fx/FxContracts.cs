using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Application.Fx;

public sealed class ConvertCurrencyRequest
{
	public string From { get; init; } = string.Empty;
	public string To { get; init; } = string.Empty;
	public long AmountMinor { get; init; }
}

public sealed class UpdateExchangeRateRequest
{
	public string BaseCurrency { get; init; } = string.Empty;
	public string QuoteCurrency { get; init; } = string.Empty;
	public decimal Rate { get; init; }
	public string Provider { get; init; } = string.Empty;
}

public sealed class FxRateResponse
{
	public string BaseCurrency { get; init; } = string.Empty;
	public string QuoteCurrency { get; init; } = string.Empty;
	public decimal Rate { get; init; }
	public string Provider { get; init; } = string.Empty;
	public DateTimeOffset ValidFrom { get; init; }
	public DateTimeOffset ValidUntil { get; init; }
	public DateTimeOffset CreatedAt { get; init; }
	public long Version { get; init; }
}

public sealed class FxConversionResponse
{
	public Guid ConversionId { get; init; }
	public CurrencyAmount SourceAmount { get; init; } = null!;
	public CurrencyAmount TargetAmount { get; init; } = null!;
	public FxRateResponse ExchangeRate { get; init; } = null!;
	public CurrencyAmount Fee { get; init; } = null!;
	public CurrencyAmount Spread { get; init; } = null!;
	public DateTimeOffset Timestamp { get; init; }
	public IReadOnlyList<FxEventType> Events { get; init; } = [];
}
