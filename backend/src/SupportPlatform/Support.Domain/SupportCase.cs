namespace Support.Domain;

public sealed class SupportCase
{
    public Guid CaseId { get; set; } = Guid.NewGuid();
    public string CaseReference { get; set; } = string.Empty;
    public string? RequesterAwidId { get; set; }
    public string? MerchantId { get; set; }
    public string? DeveloperApplicationId { get; set; }
    public SupportCaseCategory Category { get; set; } = SupportCaseCategory.Other;
    public string? Subcategory { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportCasePriority Priority { get; set; } = SupportCasePriority.Normal;
    public SupportCaseStatus Status { get; set; } = SupportCaseStatus.Open;
    public SupportCaseChannel Channel { get; set; } = SupportCaseChannel.WebPortal;
    public string? AssignedTeam { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? RelatedTransactionId { get; set; }
    public string? RelatedWalletId { get; set; }
    public string? RelatedCardId { get; set; }
    public string? RelatedSettlementId { get; set; }
    public string? RelatedComplianceCaseId { get; set; }
    public string SlaPolicyId { get; set; } = "default-v1";
    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FirstResponseAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;
    public SupportSla Sla { get; set; } = new();
    public List<SupportMessage> Messages { get; set; } = new();
    public List<SupportNote> Notes { get; set; } = new();
    public List<SupportAttachment> Attachments { get; set; } = new();
    public List<SupportAssignment> Assignments { get; set; } = new();
    public List<SupportEscalation> Escalations { get; set; } = new();
    public List<SupportTimelineEntry> Timeline { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
}
