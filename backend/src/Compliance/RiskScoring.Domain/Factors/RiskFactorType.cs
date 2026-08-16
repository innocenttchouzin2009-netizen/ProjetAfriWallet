namespace AfriWallet.Compliance.RiskScoring.Domain.Factors;

public enum RiskFactorType
{
    Kyc = 0,
    SanctionsPep = 1,
    AmlMonitoring = 2,
    Geographic = 3,
    Behavioral = 4
}