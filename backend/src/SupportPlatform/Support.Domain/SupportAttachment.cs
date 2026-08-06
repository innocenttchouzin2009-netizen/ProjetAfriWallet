namespace Support.Domain;

public sealed class SupportAttachment
{
    public Guid AttachmentId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInternalOnly { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
