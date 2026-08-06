namespace Support.Domain;

public static class SupportAuditEvent
{
    public const string SupportCaseCreated = "SUPPORT_CASE_CREATED";
    public const string SupportCaseAssigned = "SUPPORT_CASE_ASSIGNED";
    public const string SupportCaseReassigned = "SUPPORT_CASE_REASSIGNED";
    public const string SupportMessageAdded = "SUPPORT_MESSAGE_ADDED";
    public const string SupportInternalNoteAdded = "SUPPORT_INTERNAL_NOTE_ADDED";
    public const string SupportCaseEscalated = "SUPPORT_CASE_ESCALATED";
    public const string SupportSlaWarning = "SUPPORT_SLA_WARNING";
    public const string SupportSlaBreached = "SUPPORT_SLA_BREACHED";
    public const string SupportCaseResolved = "SUPPORT_CASE_RESOLVED";
    public const string SupportCaseClosed = "SUPPORT_CASE_CLOSED";
    public const string SupportCaseReopened = "SUPPORT_CASE_REOPENED";
}
