namespace Support.Domain;

public static class SupportTimelineEventType
{
    public const string CaseCreated = "CASE_CREATED";
    public const string CaseAssigned = "CASE_ASSIGNED";
    public const string MessageAdded = "MESSAGE_ADDED";
    public const string InternalNoteAdded = "INTERNAL_NOTE_ADDED";
    public const string AttachmentAdded = "ATTACHMENT_ADDED";
    public const string StatusChanged = "STATUS_CHANGED";
    public const string PriorityChanged = "PRIORITY_CHANGED";
    public const string SlaWarning = "SLA_WARNING";
    public const string SlaBreached = "SLA_BREACHED";
    public const string CaseEscalated = "CASE_ESCALATED";
    public const string CaseResolved = "CASE_RESOLVED";
    public const string CaseClosed = "CASE_CLOSED";
    public const string CaseReopened = "CASE_REOPENED";
}
