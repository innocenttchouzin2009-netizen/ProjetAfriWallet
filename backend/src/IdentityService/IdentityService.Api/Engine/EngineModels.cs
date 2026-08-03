namespace IdentityService.Api.Engine;

public enum PrivacyMode
{
    Private,
    Standard,
    Professional,
    Custom
}

public enum IdentityCardContext
{
    Personal,
    Business,
    Association
}

public enum QrType
{
    Identity,
    Payment,
    PaymentRequest,
    Business,
    Association,
    Contact
}

public sealed class IdentityAccount
{
    public string SubjectId { get; init; } = string.Empty;
    public Guid AwidId { get; init; } = Guid.CreateVersion7();
    public string Alias { get; set; } = string.Empty;
    public string PublicAwid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public List<string> VerificationBadges { get; set; } = [];
    public string PrimaryWallet { get; set; } = "Wallet •••• 4821";
    public string PreferredCurrency { get; set; } = "EUR";
    public string Theme { get; set; } = "afriwallet-premium";
    public PrivacyMode PrivacyMode { get; set; } = PrivacyMode.Private;
    public string Country { get; set; } = "CM";
    public string? City { get; set; }
    public string? BusinessName { get; set; }
    public string? AssociationName { get; set; }
    public string? BusinessHours { get; set; }
}

public sealed class DigitalIdentityCard
{
    public Guid CardId { get; init; } = Guid.CreateVersion7();
    public Guid AwidId { get; init; }
    public string Alias { get; init; } = string.Empty;
    public string PublicAwid { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfilePhoto { get; init; }
    public IReadOnlyList<string> VerificationBadges { get; init; } = Array.Empty<string>();
    public string PrimaryWallet { get; init; } = string.Empty;
    public string PreferredCurrency { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public PrivacyMode PrivacyMode { get; init; }
    public IdentityCardContext Context { get; init; }
    public string? BusinessName { get; init; }
    public string? AssociationName { get; init; }
    public string? BusinessHours { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class QrToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AwidId { get; init; }
    public string SubjectId { get; init; } = string.Empty;
    public QrType Type { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
    public int MaxUses { get; init; }
    public int UseCount { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string Token { get; set; } = string.Empty;
}

public sealed class RecipientPreview
{
    public string RecipientId { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public string PublicAwid { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Country { get; init; }
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<string> VerificationBadges { get; init; } = Array.Empty<string>();
    public string PrivacyMode { get; init; } = string.Empty;
}

public sealed class AuditEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string EventType { get; init; } = string.Empty;
    public string SubjectId { get; init; } = string.Empty;
    public Guid? QrId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Details { get; init; } = string.Empty;
}

public sealed class QrResolveResult
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public QrType? Type { get; init; }
    public string? Purpose { get; init; }
    public RecipientPreview? Recipient { get; init; }
}
