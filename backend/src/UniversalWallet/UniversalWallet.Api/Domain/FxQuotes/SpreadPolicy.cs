namespace UniversalWallet.Api.Domain.FxQuotes;

public abstract class SpreadPolicy
{
	public abstract decimal Apply(decimal marketRate, decimal spreadPercentage);
}

public sealed class PercentageSpreadPolicy : SpreadPolicy
{
	public override decimal Apply(decimal marketRate, decimal spreadPercentage) => marketRate * (1m - spreadPercentage);
}
