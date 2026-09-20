using Reconciliation.Domain.Remediation;

namespace Reconciliation.Application.Remediation;

public sealed record CreateReconciliationCorrectiveActionCommand(
    Guid ReviewId,
    string ActionCode,
    string Description,
    DateTime CreatedAtUtc);

public enum ReconciliationCorrectiveActionExecutionStatus
{
    Created = 1,
    Existing = 2,
    ResolutionNotFound = 3,
    ResolutionNotEligible = 4,
    AccessDenied = 5
}

public sealed record ReconciliationCorrectiveActionExecutionResult(
    ReconciliationCorrectiveActionExecutionStatus Status,
    ReconciliationCorrectiveAction? Action)
{
    public static ReconciliationCorrectiveActionExecutionResult Created(ReconciliationCorrectiveAction action) =>
        new(ReconciliationCorrectiveActionExecutionStatus.Created, action);

    public static ReconciliationCorrectiveActionExecutionResult Existing(ReconciliationCorrectiveAction action) =>
        new(ReconciliationCorrectiveActionExecutionStatus.Existing, action);

    public static ReconciliationCorrectiveActionExecutionResult ResolutionNotFound() =>
        new(ReconciliationCorrectiveActionExecutionStatus.ResolutionNotFound, null);

    public static ReconciliationCorrectiveActionExecutionResult ResolutionNotEligible() =>
        new(ReconciliationCorrectiveActionExecutionStatus.ResolutionNotEligible, null);

    public static ReconciliationCorrectiveActionExecutionResult AccessDenied() =>
        new(ReconciliationCorrectiveActionExecutionStatus.AccessDenied, null);
}

public enum ReconciliationCorrectiveActionLookupStatus
{
    Found = 1,
    NotFound = 2,
    AccessDenied = 3
}

public sealed record ReconciliationCorrectiveActionLookupResult(
    ReconciliationCorrectiveActionLookupStatus Status,
    ReconciliationCorrectiveAction? Action)
{
    public static ReconciliationCorrectiveActionLookupResult Found(ReconciliationCorrectiveAction action) =>
        new(ReconciliationCorrectiveActionLookupStatus.Found, action);

    public static ReconciliationCorrectiveActionLookupResult NotFound() =>
        new(ReconciliationCorrectiveActionLookupStatus.NotFound, null);

    public static ReconciliationCorrectiveActionLookupResult AccessDenied() =>
        new(ReconciliationCorrectiveActionLookupStatus.AccessDenied, null);
}
