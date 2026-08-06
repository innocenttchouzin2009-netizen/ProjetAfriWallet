using Operations.Domain;

namespace Operations.Application;

internal static class OperationsParsingExtensions
{
    public static OperationsRole ParseRole(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "SUPERADMIN" => OperationsRole.SuperAdmin,
            "OPERATIONSMANAGER" => OperationsRole.OperationsManager,
            "SUPPORTAGENT" => OperationsRole.SupportAgent,
            "SUPPORTMANAGER" => OperationsRole.SupportManager,
            "RISKANALYST" => OperationsRole.RiskAnalyst,
            "COMPLIANCEOFFICER" => OperationsRole.ComplianceOfficer,
            "FINANCEAGENT" => OperationsRole.FinanceAgent,
            "MERCHANTOPERATIONS" => OperationsRole.MerchantOperations,
            "TECHNICALOPERATOR" => OperationsRole.TechnicalOperator,
            "AUDITOR" => OperationsRole.Auditor,
            _ => OperationsRole.ReadOnly
        };
    }

    public static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "[REDACTED_EMAIL]";
        }

        return email[..1] + "***" + email[atIndex..];
    }

    public static string MaskDigits(string value)
    {
        return System.Text.RegularExpressions.Regex.Replace(value, @"\b\d{8,}\b", "[REDACTED]");
    }

    private static string Normalize(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }
}
