namespace Support.Domain;

public sealed class SupportMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public bool IsFromCustomer { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
