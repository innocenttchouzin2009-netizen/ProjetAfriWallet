using Operations.Contracts;

namespace Operations.Application;

public sealed class OperationsCenterAuthorizationService
{
    private static readonly HashSet<string> ReadRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUPER_ADMIN",
        "OPERATIONS_MANAGER",
        "TECHNICAL_OPERATOR",
        "AUDITOR",
        "READ_ONLY"
    };

    private static readonly HashSet<string> ControlRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUPER_ADMIN",
        "OPERATIONS_MANAGER",
        "TECHNICAL_OPERATOR"
    };

    public void EnsureReadAccess(OperationsContextRequest context)
    {
        if (!ReadRoles.Contains(context.Role))
        {
            throw new UnauthorizedAccessException($"Role {context.Role} cannot access the operations center.");
        }
    }

    public void EnsureControlAccess(OperationsContextRequest context, bool requireMfa = false, bool requireDeviceTrust = false)
    {
        if (!ControlRoles.Contains(context.Role))
        {
            throw new UnauthorizedAccessException($"Role {context.Role} cannot manage operations controls.");
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
}