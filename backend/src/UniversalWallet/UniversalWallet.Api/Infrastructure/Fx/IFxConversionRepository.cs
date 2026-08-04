using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public interface IFxConversionRepository
{
	void Save(FxConversion conversion);
	IReadOnlyList<FxConversion> List();
	FxConversion? Get(Guid conversionId);
}
