namespace UniversalWallet.Api.Domain.FxQuotes;

public abstract class FeePolicy
{
	public abstract decimal Calculate(decimal sourceAmountMinor, decimal targetAmountMinor, decimal marketRate);
}

public sealed class FlatFeePolicy : FeePolicy
{
	private readonly decimal _feeMinor;

	public FlatFeePolicy(decimal feeMinor)
	{
		_feeMinor = feeMinor;
	}

	public override decimal Calculate(decimal sourceAmountMinor, decimal targetAmountMinor, decimal marketRate) => _feeMinor;
}

public sealed class PercentageFeePolicy : FeePolicy
{
	private readonly decimal _percentage;

	public PercentageFeePolicy(decimal percentage)
	{
		_percentage = percentage;
	}

	public override decimal Calculate(decimal sourceAmountMinor, decimal targetAmountMinor, decimal marketRate) => sourceAmountMinor * _percentage;
}
