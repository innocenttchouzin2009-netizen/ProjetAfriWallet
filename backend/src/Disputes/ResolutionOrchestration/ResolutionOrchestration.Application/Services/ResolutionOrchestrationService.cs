using AfriWallet.Disputes.Resolution.Application.Abstractions;
using AfriWallet.Disputes.Resolution.Application.Commands;
using AfriWallet.Disputes.Resolution.Application.Policies;
using AfriWallet.Disputes.Resolution.Application.Results;
using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Application.Services;

public sealed class ResolutionOrchestrationService(
    IResolutionRepository repository,
    IDisputeDecisionReader decisions,
    IResolutionProvider provider,
    IResolutionAuditStore audit,
    IResolutionClock clock,
    ResolutionRoutingPolicy routingPolicy,
    ResolutionRetryPolicy retryPolicy)
{
    public async Task<ResolutionResult> CreateAsync(CreateResolutionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(command));

        var existing = await repository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken)
            ?? await repository.GetByDecisionAsync(command.DecisionId, cancellationToken);
        if (existing is not null)
            return Map(existing);

        var decision = await decisions.GetAsync(command.DecisionId, cancellationToken)
            ?? throw new KeyNotFoundException("Dispute decision not found.");

        var route = routingPolicy.Resolve(decision);
        var resolution = new ResolutionOrchestration(
            Guid.NewGuid(), decision.DecisionId, decision.ClaimId, decision.Awid, route, command.IdempotencyKey, clock.UtcNow);

        await repository.AddAsync(resolution, cancellationToken);
        await AuditAsync(resolution, "resolution.created", command.Actor, cancellationToken);
        return Map(resolution);
    }

    public async Task<ResolutionResult> DispatchAsync(DispatchResolutionCommand command, CancellationToken cancellationToken = default)
    {
        var resolution = await RequireAsync(command.ResolutionId, cancellationToken);
        if (resolution.Status != ResolutionStatus.Created)
            throw new InvalidOperationException("Resolution must be freshly created to dispatch.");

        await SubmitToProviderAsync(resolution, command.Actor, cancellationToken);
        return Map(resolution);
    }

    public async Task<ResolutionResult> RetryAsync(RetryResolutionCommand command, CancellationToken cancellationToken = default)
    {
        var resolution = await RequireAsync(command.ResolutionId, cancellationToken);
        if (resolution.Status != ResolutionStatus.RetryPending)
            throw new InvalidOperationException("Resolution is not pending retry.");

        await SubmitToProviderAsync(resolution, command.Actor, cancellationToken);
        return Map(resolution);
    }

    public async Task<ResolutionResult> CompensateAsync(CompensateResolutionCommand command, CancellationToken cancellationToken = default)
    {
        var resolution = await RequireAsync(command.ResolutionId, cancellationToken);
        if (resolution.Status != ResolutionStatus.CompensationRequired)
            throw new InvalidOperationException("Resolution does not require compensation.");
        if (string.IsNullOrWhiteSpace(resolution.ProviderReference))
            throw new InvalidOperationException("Provider reference is required for compensation.");

        var succeeded = await provider.CompensateAsync(resolution.ProviderReference, cancellationToken);
        if (succeeded)
        {
            resolution.CompleteCompensation(clock.UtcNow);
            await repository.SaveAsync(resolution, cancellationToken);
            await AuditAsync(resolution, "resolution.compensated", command.Actor, cancellationToken);
        }
        else
        {
            resolution.RequireManualIntervention(ResolutionReasonCode.ManualInterventionRequired, clock.UtcNow);
            await repository.SaveAsync(resolution, cancellationToken);
            await AuditAsync(resolution, "resolution.manual_intervention_required", command.Actor, cancellationToken);
        }

        return Map(resolution);
    }

    public async Task<ResolutionResult> ResolveAsync(ResolveResolutionCommand command, CancellationToken cancellationToken = default)
    {
        var resolution = await RequireAsync(command.ResolutionId, cancellationToken);
        resolution.Resolve(clock.UtcNow);
        await repository.SaveAsync(resolution, cancellationToken);
        await AuditAsync(resolution, "resolution.resolved", command.Actor, cancellationToken);
        return Map(resolution);
    }

    public async Task<ResolutionResult> GetAsync(Guid resolutionId, CancellationToken cancellationToken = default) =>
        Map(await RequireAsync(resolutionId, cancellationToken));

    private async Task SubmitToProviderAsync(ResolutionOrchestration resolution, string actor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));

        var correlationId = Guid.NewGuid().ToString("N");
        resolution.MarkDispatchPending(correlationId, clock.UtcNow);

        var result = await provider.SubmitAsync(
            new ResolutionProviderRequest(resolution.ResolutionId, resolution.DecisionId, resolution.Route, resolution.Awid, resolution.IdempotencyKey, correlationId),
            cancellationToken);

        resolution.RecordAttempt(result.ProviderReference, result.Status.ToString(), clock.UtcNow);

        string eventType;
        switch (result.Status)
        {
            case ProviderSubmissionStatus.Accepted:
                resolution.Acknowledge(result.ProviderReference, clock.UtcNow);
                eventType = "resolution.acknowledged";
                break;
            case ProviderSubmissionStatus.PartialFailure:
                resolution.RequireCompensation("Partial provider processing detected.", result.ProviderReference, clock.UtcNow);
                eventType = "resolution.compensation_required";
                break;
            case ProviderSubmissionStatus.Timeout:
                await HandleRetryableFailureAsync(resolution, ResolutionReasonCode.ProviderTimeout);
                eventType = resolution.Status == ResolutionStatus.RetryPending ? "resolution.retry_scheduled" : "resolution.manual_intervention_required";
                break;
            case ProviderSubmissionStatus.TemporaryFailure:
                await HandleRetryableFailureAsync(resolution, ResolutionReasonCode.ProviderTemporaryFailure);
                eventType = resolution.Status == ResolutionStatus.RetryPending ? "resolution.retry_scheduled" : "resolution.manual_intervention_required";
                break;
            default:
                resolution.Fail(ResolutionReasonCode.ProviderPermanentFailure, clock.UtcNow);
                eventType = "resolution.failed";
                break;
        }

        await repository.SaveAsync(resolution, cancellationToken);
        await AuditAsync(resolution, eventType, actor, cancellationToken);
    }

    private async Task HandleRetryableFailureAsync(ResolutionOrchestration resolution, ResolutionReasonCode reason)
    {
        if (retryPolicy.ShouldRetry(
                reason == ResolutionReasonCode.ProviderTimeout ? ProviderSubmissionStatus.Timeout : ProviderSubmissionStatus.TemporaryFailure,
                resolution.AttemptCount))
        {
            resolution.ScheduleRetry(reason, clock.UtcNow);
            return;
        }

        resolution.RequireManualIntervention(ResolutionReasonCode.RetryExhausted, clock.UtcNow);
        await Task.CompletedTask;
    }

    private async Task<ResolutionOrchestration> RequireAsync(Guid resolutionId, CancellationToken cancellationToken) =>
        await repository.GetAsync(resolutionId, cancellationToken)
        ?? throw new KeyNotFoundException("Resolution orchestration not found.");

    private async Task AuditAsync(ResolutionOrchestration resolution, string eventType, string actor, CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new ResolutionAuditEvent(
                Guid.NewGuid(),
                resolution.ResolutionId,
                resolution.DecisionId,
                resolution.Awid,
                eventType,
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["route"] = resolution.Route.ToString(),
                    ["status"] = resolution.Status.ToString(),
                    ["reasonCode"] = resolution.ReasonCode.ToString(),
                    ["attemptCount"] = resolution.AttemptCount.ToString(),
                    ["realRefundPerformed"] = "false",
                    ["realChargebackSubmitted"] = "false",
                    ["realMoneyMovementPerformed"] = "false",
                    ["directLedgerMutationPerformed"] = "false",
                    ["externalProviderSettlementPerformed"] = "false"
                }),
            cancellationToken);
    }

    private static ResolutionResult Map(ResolutionOrchestration resolution) => new(
        resolution.ResolutionId,
        resolution.DecisionId,
        resolution.ClaimId,
        resolution.Awid,
        resolution.Route,
        resolution.Status,
        resolution.ReasonCode,
        resolution.IdempotencyKey,
        resolution.CorrelationId,
        resolution.ProviderReference,
        resolution.AttemptCount,
        resolution.Compensations.Count,
        resolution.CreatedAtUtc,
        resolution.UpdatedAtUtc);
}
