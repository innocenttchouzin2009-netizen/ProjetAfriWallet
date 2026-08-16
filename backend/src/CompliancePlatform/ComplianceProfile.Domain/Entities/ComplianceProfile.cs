namespace AfriWallet.CompliancePlatform.ComplianceProfile.Domain;

public enum KycProfileStatus
{
    Draft,
    PendingReview,
    Active,
    Rejected,
    Suspended
}

public sealed class KycDocument
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();
    public string DocumentType { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ProviderName { get; init; } = "sandbox";
    public string Status { get; init; } = "received";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class ComplianceProfile
{
    public Guid ProfileId { get; private set; } = Guid.NewGuid();
    public string CustomerId { get; private set; }
    public string ProfileType { get; private set; }
    public string SourceChannel { get; private set; }
    public string RiskLevel { get; private set; }
    public KycProfileStatus Status { get; private set; } = KycProfileStatus.Draft;
    public string ReviewStage { get; private set; } = "not_started";
    public string? LastDecisionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public List<KycDocument> Documents { get; } = new();
    public List<string> AuditTrail { get; } = new();

    private ComplianceProfile(
        string customerId,
        string profileType,
        string sourceChannel,
        string riskLevel)
    {
        CustomerId = customerId.Trim();
        ProfileType = profileType.Trim();
        SourceChannel = sourceChannel.Trim();
        RiskLevel = riskLevel.Trim();

        if (string.IsNullOrWhiteSpace(CustomerId)) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(ProfileType)) throw new ArgumentException("ProfileType is required.", nameof(profileType));
        if (string.IsNullOrWhiteSpace(SourceChannel)) throw new ArgumentException("SourceChannel is required.", nameof(sourceChannel));

        AuditTrail.Add($"PROFILE_CREATED:{CreatedAt:O}");
    }

    public static ComplianceProfile Create(
        string customerId,
        string profileType,
        string sourceChannel,
        string? riskLevel = null)
    {
        var normalizedRisk = string.IsNullOrWhiteSpace(riskLevel) ? "low" : riskLevel.Trim();
        return new ComplianceProfile(customerId, profileType, sourceChannel, normalizedRisk);
    }

    public void AddDocument(string documentType, string reference, string countryCode, string? providerName = null)
    {
        if (string.IsNullOrWhiteSpace(documentType)) throw new ArgumentException("Document type is required.", nameof(documentType));
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Reference is required.", nameof(reference));
        if (string.IsNullOrWhiteSpace(countryCode)) throw new ArgumentException("Country code is required.", nameof(countryCode));

        Documents.Add(new KycDocument
        {
            DocumentType = documentType,
            Reference = reference,
            CountryCode = countryCode,
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? "sandbox" : providerName,
            Status = "received"
        });

        UpdatedAt = DateTimeOffset.UtcNow;
        AuditTrail.Add($"DOCUMENT_ADDED:{documentType}:{reference}");
    }

    public void SubmitForReview(string reviewer)
    {
        if (Status == KycProfileStatus.Rejected) throw new InvalidOperationException("Rejected profiles must be recreated.");

        Status = KycProfileStatus.PendingReview;
        ReviewStage = string.IsNullOrWhiteSpace(reviewer) ? "manual_review" : $"manual_review:{reviewer}";
        UpdatedAt = DateTimeOffset.UtcNow;
        AuditTrail.Add($"SUBMITTED_FOR_REVIEW:{reviewer ?? "system"}");
    }

    public void Approve(string reviewer, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Decision reason is required.", nameof(reason));

        Status = KycProfileStatus.Active;
        ReviewStage = "approved";
        LastDecisionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
        AuditTrail.Add($"PROFILE_APPROVED:{reviewer}:{reason}");
    }

    public void Reject(string reviewer, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Decision reason is required.", nameof(reason));

        Status = KycProfileStatus.Rejected;
        ReviewStage = "rejected";
        LastDecisionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
        AuditTrail.Add($"PROFILE_REJECTED:{reviewer}:{reason}");
    }

    public void Suspend(string reviewer, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Decision reason is required.", nameof(reason));

        Status = KycProfileStatus.Suspended;
        ReviewStage = "suspended";
        LastDecisionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
        AuditTrail.Add($"PROFILE_SUSPENDED:{reviewer}:{reason}");
    }
}
