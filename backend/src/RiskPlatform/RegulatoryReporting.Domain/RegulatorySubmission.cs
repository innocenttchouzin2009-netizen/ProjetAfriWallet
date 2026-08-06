namespace RegulatoryReporting.Domain;

public sealed class RegulatorySubmission
{
    public Guid SubmissionId { get; init; } = Guid.NewGuid();
    public string AuthorityCode { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "SUBMITTED";
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
}
