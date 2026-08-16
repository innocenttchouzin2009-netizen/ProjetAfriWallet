using AfriWallet.CompliancePlatform.ComplianceProfile.Domain;

namespace AfriWallet.CompliancePlatform.ComplianceProfile.Application;

public sealed record CreateComplianceProfileRequest(
    string CustomerId,
    string ProfileType,
    string SourceChannel,
    string? RiskLevel = null);

public sealed record AddDocumentRequest(
    Guid ProfileId,
    string DocumentType,
    string Reference,
    string CountryCode,
    string? ProviderName = null);

public sealed record ReviewComplianceProfileRequest(
    Guid ProfileId,
    string Reviewer,
    bool IsApproved,
    string DecisionReason,
    string? Notes = null);

public sealed record ComplianceProfileDocumentView(
    Guid DocumentId,
    string DocumentType,
    string Reference,
    string CountryCode,
    string ProviderName,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ComplianceProfileView(
    Guid ProfileId,
    string CustomerId,
    string ProfileType,
    string SourceChannel,
    string RiskLevel,
    string Status,
    string ReviewStage,
    string? LastDecisionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ComplianceProfileDocumentView> Documents,
    IReadOnlyCollection<string> AuditTrail);
