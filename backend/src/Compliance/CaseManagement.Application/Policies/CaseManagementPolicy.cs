using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Application.Policies;

public sealed class CaseManagementPolicy
{
    public ComplianceCasePriority ResolvePriority(CaseSourceType sourceType) => sourceType switch
    {
        CaseSourceType.SanctionsPep => ComplianceCasePriority.Critical,
        CaseSourceType.AmlMonitoring => ComplianceCasePriority.High,
        CaseSourceType.FinancialRisk => ComplianceCasePriority.Medium,
        _ => ComplianceCasePriority.Low
    };
}