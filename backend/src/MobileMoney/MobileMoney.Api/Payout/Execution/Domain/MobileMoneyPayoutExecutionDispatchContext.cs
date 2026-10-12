namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionDispatchContext
{
    private MobileMoneyPayoutExecutionDispatchContext(
        MobileMoneyPayoutExecutionBinding binding,
        MobileMoneyPayoutExecutionBeneficiaryBinding beneficiary)
    {
        Binding = binding;
        Beneficiary = beneficiary;
    }

    public MobileMoneyPayoutExecutionBinding Binding { get; }
    public MobileMoneyPayoutExecutionBeneficiaryBinding Beneficiary { get; }

    public static MobileMoneyPayoutExecutionDispatchContext Create(
        MobileMoneyPayoutExecutionBinding binding,
        MobileMoneyPayoutExecutionBeneficiaryBinding beneficiary)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(beneficiary);

        if (!string.Equals(
                binding.Quote.DestinationCountryCode,
                beneficiary.CountryCode,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Payout quote destination country and beneficiary country must match.");
        }

        if (!string.Equals(
                binding.Quote.OperatorCode,
                beneficiary.OperatorCode,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Payout quote operator and beneficiary operator must match.");
        }

        return new MobileMoneyPayoutExecutionDispatchContext(binding, beneficiary);
    }
}
