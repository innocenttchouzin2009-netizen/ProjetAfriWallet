namespace IdentityService.Domain.Entities;

public sealed class Awid
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string SubjectId { get; set; } = string.Empty;
    public string PublicAwid { get; set; } = string.Empty;
    public string AliasCanonical { get; set; } = string.Empty;
    public string AliasDisplay { get; set; } = string.Empty;
    public AwidStatus Status { get; set; } = AwidStatus.Active;
    public AwidPrivacyMode PrivacyMode { get; set; } = AwidPrivacyMode.Private;
    public string IssuedMarket { get; set; } = "237";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastAliasChangedAt { get; set; }
    public int Version { get; set; } = 1;
}

public enum AwidStatus
{
    Pending,
    Active,
    Limited,
    Suspended,
    Retired,
    Closed
}

public enum AwidPrivacyMode
{
    Private,
    Standard,
    Professional,
    Custom
}
