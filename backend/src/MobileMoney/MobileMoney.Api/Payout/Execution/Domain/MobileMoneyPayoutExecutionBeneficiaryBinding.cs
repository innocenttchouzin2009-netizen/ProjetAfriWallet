using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionBeneficiaryBinding
{
    private MobileMoneyPayoutExecutionBeneficiaryBinding(
        string msisdn,
        string countryCode,
        string operatorCode,
        string? displayName)
    {
        Msisdn = msisdn;
        CountryCode = countryCode;
        OperatorCode = operatorCode;
        DisplayName = displayName;
    }

    public string Msisdn { get; }
    public string CountryCode { get; }
    public string OperatorCode { get; }
    public string? DisplayName { get; }

    public static MobileMoneyPayoutExecutionBeneficiaryBinding Create(
        MobileMoneyBeneficiary beneficiary)
    {
        ArgumentNullException.ThrowIfNull(beneficiary);

        return new MobileMoneyPayoutExecutionBeneficiaryBinding(
            beneficiary.Msisdn,
            beneficiary.CountryCode,
            beneficiary.OperatorCode,
            beneficiary.DisplayName);
    }
}
