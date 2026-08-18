using AfriWallet.Disputes.Decision.Application.Abstractions;
using AfriWallet.Disputes.Decision.Application.Commands;
using AfriWallet.Disputes.Decision.Application.Results;
using AfriWallet.Disputes.Decision.Domain.Decisions;

namespace AfriWallet.Disputes.Decision.Application.Services;

public sealed class DisputeDecisionService(
    IDisputeDecisionRepository repository,
    IInvestigationOutcomeReader investigations,
    IDisputeDecisionAuditStore audit,
    IDisputeDecisionClock clock,
    DisputeDecisionPolicy policy)
{
    public async Task<DisputeDecisionResult> EvaluateAsync(EvaluateDisputeDecisionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var existing = await repository.GetActiveByInvestigationAsync(command.InvestigationId, cancellationToken);
        if (existing is not null)
            return Map(existing);

        var investigation = await RequireInvestigationAsync(command.InvestigationId, cancellationToken);
        var decision = CreateDecision(investigation, command.Actor);
        await repository.AddAsync(decision, cancellationToken);
        await AuditAsync(decision, "decision.evaluated", command.Actor, cancellationToken);
        return Map(decision);
    }

    public async Task<DisputeDecisionResult> ApproveAsync(ApproveDisputeDecisionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var decision = await RequireDecisionAsync(command.DecisionId, cancellationToken);
        decision.Approve(command.Approver, command.Note, clock.UtcNow);
        await repository.SaveAsync(decision, cancellationToken);
        await AuditAsync(decision, "decision.approved", command.Actor, cancellationToken);
        return Map(decision);
    }

    public async Task<DisputeDecisionResult> ReevaluateAsync(ReevaluateDisputeDecisionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new ArgumentException("Reevaluation reason is required.", nameof(command));

        var investigation = await RequireInvestigationAsync(command.InvestigationId, cancellationToken);

        var current = await repository.GetActiveByInvestigationAsync(command.InvestigationId, cancellationToken);
        if (current is not null)
        {
            current.Supersede(clock.UtcNow);
            await repository.SaveAsync(current, cancellationToken);
            await AuditAsync(current, "decision.superseded", command.Actor, cancellationToken, command.Reason);
        }

        var decision = CreateDecision(investigation, command.Actor);
        await repository.AddAsync(decision, cancellationToken);
        await AuditAsync(decision, "decision.reevaluated", command.Actor, cancellationToken, command.Reason);
        return Map(decision);
    }

    public async Task<DisputeDecisionResult> GetAsync(Guid decisionId, CancellationToken cancellationToken = default) =>
        Map(await RequireDecisionAsync(decisionId, cancellationToken));

    private DisputeResolutionDecision CreateDecision(InvestigationOutcomeSnapshot investigation, string actor)
    {
        var evaluation = policy.Evaluate(investigation);
        return new DisputeResolutionDecision(
            Guid.NewGuid(),
            investigation.ClaimId,
            investigation.InvestigationId,
            investigation.Awid,
            evaluation.DecisionType,
            evaluation.ReasonCode,
            evaluation.PolicyVersion,
            evaluation.RequiresManualApproval,
            evaluation.Factors,
            clock.UtcNow);
    }

    private async Task<InvestigationOutcomeSnapshot> RequireInvestigationAsync(Guid investigationId, CancellationToken cancellationToken)
    {
        var investigation = await investigations.GetAsync(investigationId, cancellationToken)
            ?? throw new KeyNotFoundException("Investigation not found.");

        if (!string.Equals(investigation.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Investigation must be completed before a decision can be evaluated.");

        return investigation;
    }

    private async Task<DisputeResolutionDecision> RequireDecisionAsync(Guid decisionId, CancellationToken cancellationToken) =>
        await repository.GetAsync(decisionId, cancellationToken)
        ?? throw new KeyNotFoundException("Decision not found.");

    private async Task AuditAsync(
        DisputeResolutionDecision decision,
        string eventType,
        string actor,
        CancellationToken cancellationToken,
        string? reason = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["decisionType"] = decision.DecisionType.ToString(),
            ["status"] = decision.Status.ToString(),
            ["reasonCode"] = decision.ReasonCode.ToString(),
            ["policyVersion"] = decision.PolicyVersion.ToString(),
            ["refundExecuted"] = "false",
            ["chargebackExecuted"] = "false",
            ["moneyMovementPerformed"] = "false",
            ["ledgerMutationPerformed"] = "false"
        };
        if (!string.IsNullOrWhiteSpace(reason))
            metadata["reevaluationReason"] = reason;

        await audit.AppendAsync(
            new DisputeDecisionAuditEvent(
                Guid.NewGuid(),
                decision.DecisionId,
                decision.ClaimId,
                decision.InvestigationId,
                decision.Awid,
                eventType,
                actor,
                clock.UtcNow,
                metadata),
            cancellationToken);
    }

    private static DisputeDecisionResult Map(DisputeResolutionDecision decision) => new(
        decision.DecisionId,
        decision.ClaimId,
        decision.InvestigationId,
        decision.Awid,
        decision.DecisionType,
        decision.Status,
        decision.ReasonCode,
        decision.PolicyVersion.ToString(),
        decision.RequiresManualApproval,
        decision.Factors.Count,
        decision.CreatedAtUtc,
        decision.UpdatedAtUtc);
}
