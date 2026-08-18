using AfriWallet.Disputes.Investigation.Application.Abstractions;
using AfriWallet.Disputes.Investigation.Application.Commands;
using AfriWallet.Disputes.Investigation.Application.Results;
using AfriWallet.Disputes.Investigation.Domain.Cases;
using AfriWallet.Disputes.Investigation.Domain.Evidence;
using AfriWallet.Disputes.Investigation.Domain.Requests;

namespace AfriWallet.Disputes.Investigation.Application.Services;

public sealed class DisputeInvestigationService(
    IDisputeInvestigationRepository repository,
    IDisputeEligibilityReader eligibility,
    IDisputeInvestigationAuditStore audit,
    IDisputeInvestigationClock clock)
{
    public async Task<InvestigationResult> CreateAsync(CreateInvestigationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ClaimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var snapshot = await eligibility.GetByClaimAsync(command.ClaimId, cancellationToken)
            ?? throw new InvalidOperationException("Dispute eligibility decision is required before investigation.");

        if (!string.Equals(snapshot.Status, "Eligible", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(snapshot.Status, "ManualReviewRequired", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Claim is not eligible for investigation (status: {snapshot.Status}).");

        var now = clock.UtcNow;
        var investigation = new DisputeInvestigationCase(Guid.NewGuid(), snapshot.ClaimId, snapshot.Awid, command.Actor, now);
        await repository.AddAsync(investigation, cancellationToken);
        await AuditAsync(investigation, "investigation.created", command.Actor, cancellationToken);
        return Map(investigation);
    }

    public Task<InvestigationResult> AssignAsync(AssignInvestigationCommand command, CancellationToken ct = default) =>
        MutateAsync(command.InvestigationId, command.Actor, "investigation.assigned",
            (investigation, now) => investigation.Assign(command.AnalystId, command.Actor, now), ct);

    public Task<InvestigationResult> RequestEvidenceAsync(RequestEvidenceCommand command, CancellationToken ct = default) =>
        MutateAsync(command.InvestigationId, command.Actor, "evidence.requested",
            (investigation, now) => investigation.RequestEvidence(command.Type, command.RequestedFrom, command.Reason, command.Actor, now), ct);

    public Task<InvestigationResult> AddEvidenceAsync(AddEvidenceCommand command, CancellationToken ct = default) =>
        MutateAsync(command.InvestigationId, command.Actor, "evidence.added",
            (investigation, now) => investigation.AddEvidence(
                command.Type,
                command.Reference,
                command.Description,
                new EvidenceIntegrity(command.Sha256, command.SizeBytes, command.ContentType),
                command.SubmittedBy,
                command.Actor,
                now), ct);

    public Task<InvestigationResult> CompleteAsync(CompleteInvestigationCommand command, CancellationToken ct = default) =>
        MutateAsync(command.InvestigationId, command.Actor, "investigation.completed",
            (investigation, now) => investigation.Complete(command.Outcome, command.Actor, now), ct);

    public Task<InvestigationResult> CloseAsync(Guid investigationId, string actor, CancellationToken ct = default) =>
        MutateAsync(investigationId, actor, "investigation.closed", (investigation, now) => investigation.Close(actor, now), ct);

    public async Task<InvestigationResult> GetAsync(Guid investigationId, CancellationToken cancellationToken = default) =>
        Map(await GetRequiredAsync(investigationId, cancellationToken));

    private async Task<InvestigationResult> MutateAsync(
        Guid investigationId,
        string actor,
        string eventType,
        Action<DisputeInvestigationCase, DateTimeOffset> mutation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));

        var investigation = await GetRequiredAsync(investigationId, cancellationToken);
        mutation(investigation, clock.UtcNow);
        await repository.SaveAsync(investigation, cancellationToken);
        await AuditAsync(investigation, eventType, actor, cancellationToken);
        return Map(investigation);
    }

    private async Task<DisputeInvestigationCase> GetRequiredAsync(Guid investigationId, CancellationToken cancellationToken) =>
        await repository.GetAsync(investigationId, cancellationToken)
        ?? throw new KeyNotFoundException("Investigation not found.");

    private async Task AuditAsync(DisputeInvestigationCase investigation, string eventType, string actor, CancellationToken cancellationToken) =>
        await audit.AppendAsync(
            new DisputeInvestigationAuditEvent(
                Guid.NewGuid(),
                investigation.InvestigationId,
                investigation.ClaimId,
                investigation.Awid,
                eventType,
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["status"] = investigation.Status.ToString(),
                    ["outcome"] = investigation.Outcome.ToString(),
                    ["refundDecisionPerformed"] = "false",
                    ["chargebackPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false"
                }),
            cancellationToken);

    private static InvestigationResult Map(DisputeInvestigationCase investigation) => new(
        investigation.InvestigationId,
        investigation.ClaimId,
        investigation.Awid,
        investigation.AnalystId,
        investigation.Status,
        investigation.Outcome,
        investigation.Evidence.Count,
        investigation.Requests.Count(x => x.Status == EvidenceRequestStatus.Open),
        investigation.Timeline.Count,
        investigation.CreatedAtUtc,
        investigation.UpdatedAtUtc);
}
