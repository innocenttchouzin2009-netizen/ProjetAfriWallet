using AfriWallet.Fraud.Investigation.Application.Abstractions;
using AfriWallet.Fraud.Investigation.Application.Policies;
using AfriWallet.Fraud.Investigation.Domain.Cases;
using AfriWallet.Fraud.Investigation.Domain.Evidence;
using AfriWallet.Fraud.Investigation.Domain.Notes;
using AfriWallet.Fraud.Investigation.Domain.Responses;

namespace AfriWallet.Fraud.Investigation.Application.Cases;

public sealed class FraudInvestigationService(
    IFraudCaseRepository repository,
    IFraudInvestigationAuditStore audit,
    IFraudDecisionEvidenceReader decisions,
    IFraudInvestigationClock clock,
    FraudResponsePolicy responsePolicy)
{
    public async Task<FraudCaseResult> CreateAsync(CreateFraudCaseCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await decisions.GetByTransactionAsync(command.TransactionId, cancellationToken)
            ?? throw new InvalidOperationException("Fraud decision evidence is required.");
        if (!string.Equals(snapshot.Awid.Trim(), command.Awid.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fraud decision AWID does not match the case AWID.");

        var now = clock.UtcNow;
        var fraudCase = new FraudCase(Guid.NewGuid(), command.Awid, command.TransactionId, command.Title, command.Priority, now);
        fraudCase.AddEvidence(new FraudEvidence(snapshot.DecisionId, FraudEvidenceType.FraudDecision, snapshot.DecisionId.ToString(), $"Decision {snapshot.Action} at score {snapshot.Score} ({snapshot.Band})", snapshot.DecidedAtUtc), now);
        fraudCase.AddResponseRecommendation(new FraudResponseRecommendation(responsePolicy.Recommend(snapshot.Score, snapshot.Action), $"Initial recommendation from fraud decision action {snapshot.Action}.", now), now);
        await repository.AddAsync(fraudCase, cancellationToken);
        await PersistAndAuditAsync(fraudCase, "case.created", command.Actor, cancellationToken, save: false);
        return Map(fraudCase);
    }

    public Task<FraudCaseResult> AssignAsync(AssignFraudCaseCommand command, CancellationToken ct = default) => MutateAsync(command.CaseId, command.Actor, "case.assigned", (c, now) => c.Assign(command.AnalystId, command.Actor, now), ct);
    public Task<FraudCaseResult> StartInvestigationAsync(Guid caseId, string actor, CancellationToken ct = default) => MutateAsync(caseId, actor, "case.investigation_started", (c, now) => c.StartInvestigation(now), ct);
    public Task<FraudCaseResult> AddNoteAsync(AddFraudCaseNoteCommand command, CancellationToken ct = default) => MutateAsync(command.CaseId, command.Actor, "case.note_added", (c, now) => c.AddNote(new FraudInvestigationNote(Guid.NewGuid(), command.Actor, command.Content, now), now), ct);
    public Task<FraudCaseResult> EscalateAsync(EscalateFraudCaseCommand command, CancellationToken ct = default) => MutateAsync(command.CaseId, command.Actor, "case.escalated", (c, now) => c.Escalate(command.Priority, now), ct);
    public Task<FraudCaseResult> AddResponseAsync(AddFraudResponseCommand command, CancellationToken ct = default) => MutateAsync(command.CaseId, command.Actor, "case.response_recommended", (c, now) => c.AddResponseRecommendation(new FraudResponseRecommendation(command.ResponseType, command.Reason, now), now), ct);
    public Task<FraudCaseResult> ResolveAsync(ResolveFraudCaseCommand command, CancellationToken ct = default) => MutateAsync(command.CaseId, command.Actor, "case.resolved", (c, now) => c.Resolve(command.Resolution, now), ct);
    public Task<FraudCaseResult> CloseAsync(Guid caseId, string actor, CancellationToken ct = default) => MutateAsync(caseId, actor, "case.closed", (c, now) => c.Close(now), ct);

    private async Task<FraudCaseResult> MutateAsync(Guid caseId, string actor, string eventType, Action<FraudCase, DateTimeOffset> mutation, CancellationToken ct)
    {
        var fraudCase = await repository.GetAsync(caseId, ct) ?? throw new InvalidOperationException("Fraud case was not found.");
        mutation(fraudCase, clock.UtcNow);
        await PersistAndAuditAsync(fraudCase, eventType, actor, ct, save: true);
        return Map(fraudCase);
    }

    private async Task PersistAndAuditAsync(FraudCase fraudCase, string eventType, string actor, CancellationToken ct, bool save)
    {
        if (save) await repository.SaveAsync(fraudCase, ct);
        await audit.AppendAsync(new FraudInvestigationAuditEvent(Guid.NewGuid(), fraudCase.CaseId, fraudCase.Awid, fraudCase.TransactionId, eventType, actor, clock.UtcNow, new Dictionary<string, string>
        {
            ["status"] = fraudCase.Status.ToString(), ["priority"] = fraudCase.Priority.ToString(), ["resolution"] = fraudCase.Resolution.ToString(), ["executionPerformed"] = "false"
        }), ct);
    }

    private static FraudCaseResult Map(FraudCase fraudCase) => new(fraudCase.CaseId, fraudCase.Awid, fraudCase.TransactionId, fraudCase.Title, fraudCase.Priority, fraudCase.Status, fraudCase.Resolution, fraudCase.Assignment?.AnalystId, fraudCase.Evidence.Count, fraudCase.Notes.Count, fraudCase.Responses.Count, fraudCase.CreatedAtUtc, fraudCase.UpdatedAtUtc);
}