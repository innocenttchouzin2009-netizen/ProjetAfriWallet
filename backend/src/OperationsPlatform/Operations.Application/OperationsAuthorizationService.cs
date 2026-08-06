using Operations.Contracts;
using Operations.Domain;

namespace Operations.Application;

public sealed class OperationsAuthorizationService
{
    private readonly Dictionary<OperationsRole, OperationsPermission> _permissions = new();

    public OperationsAuthorizationService()
    {
        Register(OperationsRole.SuperAdmin, true, true, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewHealth, OperationsAction.ViewAudit, OperationsAction.SuspendWallet, OperationsAction.FreezeCard, OperationsAction.AssignCase, OperationsAction.RetryTransaction, OperationsAction.ExportReport, OperationsAction.ManageFeatureFlags);
        Register(OperationsRole.OperationsManager, true, true, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewHealth, OperationsAction.ViewAudit, OperationsAction.SuspendWallet, OperationsAction.FreezeCard, OperationsAction.AssignCase, OperationsAction.RetryTransaction, OperationsAction.ExportReport);
        Register(OperationsRole.SupportAgent, false, false, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.AssignCase);
        Register(OperationsRole.SupportManager, true, false, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.AssignCase, OperationsAction.ViewAudit);
        Register(OperationsRole.RiskAnalyst, true, true, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewHealth, OperationsAction.ViewAudit, OperationsAction.ManageFeatureFlags);
        Register(OperationsRole.ComplianceOfficer, true, true, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewAudit);
        Register(OperationsRole.FinanceAgent, true, true, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewTransaction, OperationsAction.ViewAudit, OperationsAction.ExportReport);
        Register(OperationsRole.MerchantOperations, true, false, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewAudit, OperationsAction.ExportReport);
        Register(OperationsRole.TechnicalOperator, true, true, OperationsAction.ViewDashboard, OperationsAction.ViewHealth, OperationsAction.ViewAudit, OperationsAction.RetryTransaction, OperationsAction.ManageFeatureFlags);
        Register(OperationsRole.Auditor, false, false, OperationsAction.ViewDashboard, OperationsAction.ViewAudit, OperationsAction.ViewUser, OperationsAction.ViewTransaction);
        Register(OperationsRole.ReadOnly, false, false, OperationsAction.ViewDashboard, OperationsAction.SearchGlobal, OperationsAction.ViewUser, OperationsAction.ViewTransaction, OperationsAction.ViewHealth, OperationsAction.ViewAudit);
    }

    public void EnsureAllowed(OperationsContextRequest context, OperationsAction action, bool requireMfa = false, bool requireDeviceTrust = false)
    {
        var role = OperationsParsingExtensions.ParseRole(context.Role);
        var permission = _permissions[role];
        if (!permission.AllowedActions.Contains(action))
        {
            throw new UnauthorizedAccessException($"Role {role} is not allowed to perform {action}.");
        }

        if (requireMfa && !context.HasMfa)
        {
            throw new UnauthorizedAccessException("MFA is required for this action.");
        }

        if (requireDeviceTrust && !context.HasDeviceTrust)
        {
            throw new UnauthorizedAccessException("Device trust is required for this action.");
        }
    }

    public bool RequiresMfa(OperationsRole role) => _permissions[role].RequiresMfa;

    public bool RequiresDeviceTrust(OperationsRole role) => _permissions[role].RequiresDeviceTrust;

    private void Register(OperationsRole role, bool requiresMfa, bool requiresDeviceTrust, params OperationsAction[] actions)
    {
        _permissions[role] = new OperationsPermission
        {
            Role = role,
            RequiresMfa = requiresMfa,
            RequiresDeviceTrust = requiresDeviceTrust,
            AllowedActions = actions.ToHashSet()
        };
    }
}
