using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class InMemoryFxConversionRepository : IFxConversionRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, FxConversion> _conversions = new();

	public void Save(FxConversion conversion)
	{
		lock (_sync)
		{
			_conversions[conversion.ConversionId] = conversion;
		}
	}

	public IReadOnlyList<FxConversion> List()
	{
		lock (_sync)
		{
			return _conversions.Values.OrderByDescending(conversion => conversion.Timestamp).ToList();
		}
	}

	public FxConversion? Get(Guid conversionId)
	{
		lock (_sync)
		{
			return _conversions.GetValueOrDefault(conversionId);
		}
	}
}
