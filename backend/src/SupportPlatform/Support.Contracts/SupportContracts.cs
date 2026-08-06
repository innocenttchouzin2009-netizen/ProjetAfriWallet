namespace Support.Contracts;

public sealed class CreateSupportCaseRequest
{
    public string? RequesterAwidId { get; set; }
    public string? MerchantId { get; set; }
    public string? DeveloperApplicationId { get; set; }
    public string Category { get; set; } = "OTHER";
    public string? Subcategory { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "NORMAL";
    public string Channel { get; set; } = "WEB_PORTAL";
    public string? RelatedTransactionId { get; set; }
    public string? RelatedWalletId { get; set; }
    public string? RelatedCardId { get; set; }
    public string? RelatedSettlementId { get; set; }
    public string? RelatedComplianceCaseId { get; set; }
    public DateTimeOffset? OpenedAtUtcOverride { get; set; }
}

public sealed class UpdateSupportCaseRequest
{
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
}

public sealed class AssignCaseRequest
{
    public string AssignedTeam { get; set; } = string.Empty;
    public string? AssignedAgentId { get; set; }
}

public sealed class AddSupportMessageRequest
{
    public string AuthorId { get; set; } = string.Empty;
    public bool IsFromCustomer { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class AddSupportNoteRequest
{
    public string AuthorAgentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class AddSupportAttachmentRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInternalOnly { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}

public sealed class EscalateCaseRequest
{
    public string Level { get; set; } = "L2";
    public string Reason { get; set; } = string.Empty;
}

public sealed class ResolveCaseRequest
{
    public string ResolvedByAgentId { get; set; } = string.Empty;
    public string ResolutionSummary { get; set; } = string.Empty;
}

public sealed class CloseCaseRequest
{
    public string ClosedByAgentId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ReopenCaseRequest
{
    public string ReopenedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class SupportCaseResponse
{
    public Guid CaseId { get; set; }
    public string CaseReference { get; set; } = string.Empty;
    public string? RequesterAwidId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? AssignedTeam { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? RelatedComplianceCaseId { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? FirstResponseAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public int Version { get; set; }
    public SupportSlaResponse Sla { get; set; } = new();
    public List<SupportTimelineEntryResponse> Timeline { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
    public Dictionary<string, long> Telemetry { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SupportSlaResponse
{
    public string PolicyId { get; set; } = string.Empty;
    public long FirstResponseTargetMinutes { get; set; }
    public long ResolutionTargetMinutes { get; set; }
    public bool WarningTriggered { get; set; }
    public bool Breached { get; set; }
    public List<string> Violations { get; set; } = new();
}

public sealed class SupportTimelineEntryResponse
{
    public string EventType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupportMessageResponse
{
    public Guid MessageId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public bool IsFromCustomer { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupportNoteResponse
{
    public Guid NoteId { get; set; }
    public string AuthorAgentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupportAttachmentResponse
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInternalOnly { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupportCaseQuery
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public string? AssignedTeam { get; set; }
}
