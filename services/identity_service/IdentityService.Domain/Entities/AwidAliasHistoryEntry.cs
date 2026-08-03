namespace IdentityService.Domain.Entities;

public sealed class AwidAliasHistoryEntry
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AwidId { get; init; }
    public string PreviousAlias { get; init; } = string.Empty;
    public string NewAlias { get; init; } = string.Empty;
    public DateTimeOffset ChangedAt { get; init; }
    public string ChangedBy { get; init; } = "SUBJECT";
    public string Reason { get; init; } = "USER_REQUEST";
    public DateTimeOffset ReservedUntil { get; init; }
}
