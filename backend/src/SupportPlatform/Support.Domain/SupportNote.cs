namespace Support.Domain;

public sealed class SupportNote
{
    public Guid NoteId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string AuthorAgentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
