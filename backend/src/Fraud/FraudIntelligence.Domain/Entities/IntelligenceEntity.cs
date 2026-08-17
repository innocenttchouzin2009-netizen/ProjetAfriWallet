namespace AfriWallet.Fraud.Intelligence.Domain.Entities;

public sealed record IntelligenceEntity
{
    public IntelligenceEntity(string entityId, IntelligenceEntityType type, string displayReference)
    {
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity id is required.");
        if (string.IsNullOrWhiteSpace(displayReference)) throw new ArgumentException("Display reference is required.");
        EntityId = entityId.Trim();
        Type = type;
        DisplayReference = displayReference.Trim();
    }

    public string EntityId { get; }
    public IntelligenceEntityType Type { get; }
    public string DisplayReference { get; }
}