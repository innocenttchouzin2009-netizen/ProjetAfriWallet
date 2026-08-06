namespace Compliance.Domain;

public sealed class InvestigatorNote
{
    public Guid NoteId { get; init; } = Guid.NewGuid();
    public string Author { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
