namespace Reconciliation.Domain.Resolutions;

public sealed class ReconciliationResolutionAccessScope
{
    private readonly HashSet<string> partnerIds;

    private ReconciliationResolutionAccessScope(
        string actorId,
        bool canAccessAllPartners,
        IEnumerable<string> partnerIds)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            throw new ArgumentException("Actor id is required.", nameof(actorId));

        ActorId = actorId.Trim();
        CanAccessAllPartners = canAccessAllPartners;
        this.partnerIds = partnerIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.Ordinal);
    }

    public string ActorId { get; }
    public bool CanAccessAllPartners { get; }
    public IReadOnlyCollection<string> PartnerIds => partnerIds;

    public bool AllowsPartner(string partnerId)
    {
        if (string.IsNullOrWhiteSpace(partnerId))
            return false;

        return CanAccessAllPartners || partnerIds.Contains(partnerId.Trim());
    }

    public static ReconciliationResolutionAccessScope ForAllPartners(string actorId) =>
        new(actorId, true, Array.Empty<string>());

    public static ReconciliationResolutionAccessScope ForPartners(
        string actorId,
        IEnumerable<string> partnerIds)
    {
        ArgumentNullException.ThrowIfNull(partnerIds);
        return new ReconciliationResolutionAccessScope(actorId, false, partnerIds);
    }
}
