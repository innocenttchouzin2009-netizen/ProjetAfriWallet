using AfriWallet.Disputes.Registry.Application.Abstractions;
using AfriWallet.Disputes.Registry.Domain.Claims;
using AfriWallet.Disputes.Registry.Domain.Evidence;

namespace AfriWallet.Disputes.Registry.Application.Claims;

public sealed class DisputeRegistryService(
    IDisputeClaimRepository repository,
    IDisputeRegistryAuditStore audit,
    IDisputeRegistryClock clock)
{
    public async Task<DisputeClaimResult> RegisterAsync(RegisterDisputeClaimCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var now = clock.UtcNow;
        var claim = new DisputeClaim(
            Guid.NewGuid(),
            command.Awid,
            command.TransactionId,
            command.Type,
            command.Reason,
            new DisputeClaimAmount(command.AmountMinor, command.Currency),
            command.Description,
            command.SourceChannel,
            new DisputeClaimReferences(command.PaymentReference, command.BankTransferReference, command.MerchantReference),
            now);

        await repository.AddAsync(claim, cancellationToken);
        await AuditAsync(claim, "claim.registered", command.Actor, cancellationToken);
        return Map(claim);
    }

    public Task<DisputeClaimResult> SubmitAsync(Guid claimId, string actor, CancellationToken ct = default) =>
        MutateAsync(claimId, actor, "claim.submitted", (claim, now) => claim.Submit(actor, now), ct);

    public Task<DisputeClaimResult> OpenAsync(Guid claimId, string actor, CancellationToken ct = default) =>
        MutateAsync(claimId, actor, "claim.opened", (claim, now) => claim.Open(actor, now), ct);

    public Task<DisputeClaimResult> StartReviewAsync(Guid claimId, string actor, CancellationToken ct = default) =>
        MutateAsync(claimId, actor, "claim.review_started", (claim, now) => claim.StartReview(actor, now), ct);

    public Task<DisputeClaimResult> LinkEvidenceAsync(LinkDisputeEvidenceCommand command, CancellationToken ct = default) =>
        MutateAsync(
            command.ClaimId,
            command.Actor,
            "claim.evidence_linked",
            (claim, now) => claim.AddEvidenceReference(
                new DisputeEvidenceReference(Guid.NewGuid(), command.Type, command.ReferenceId, command.Summary, now),
                now),
            ct);

    public Task<DisputeClaimResult> ResolveAsync(ResolveDisputeClaimCommand command, CancellationToken ct = default) =>
        MutateAsync(command.ClaimId, command.Actor, "claim.resolved", (claim, now) => claim.Resolve(command.Outcome, command.Actor, now), ct);

    public Task<DisputeClaimResult> CloseAsync(Guid claimId, string actor, CancellationToken ct = default) =>
        MutateAsync(claimId, actor, "claim.closed", (claim, now) => claim.Close(actor, now), ct);

    public Task<DisputeClaimResult> RejectAsync(RejectDisputeClaimCommand command, CancellationToken ct = default) =>
        MutateAsync(command.ClaimId, command.Actor, "claim.rejected", (claim, now) => claim.Reject(command.Reason, command.Actor, now), ct);

    public Task<DisputeClaimResult> CancelAsync(CancelDisputeClaimCommand command, CancellationToken ct = default) =>
        MutateAsync(command.ClaimId, command.Actor, "claim.cancelled", (claim, now) => claim.Cancel(command.Reason, command.Actor, now), ct);

    public async Task<IReadOnlyCollection<DisputeClaimResult>> GetByAwidAsync(string awid, CancellationToken ct = default)
    {
        var claims = await repository.GetByAwidAsync(awid, ct);
        return claims.Select(Map).ToArray();
    }

    private async Task<DisputeClaimResult> MutateAsync(
        Guid claimId,
        string actor,
        string eventType,
        Action<DisputeClaim, DateTimeOffset> mutation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));

        var claim = await repository.GetAsync(claimId, cancellationToken)
            ?? throw new InvalidOperationException("Dispute claim was not found.");

        mutation(claim, clock.UtcNow);
        await repository.SaveAsync(claim, cancellationToken);
        await AuditAsync(claim, eventType, actor, cancellationToken);
        return Map(claim);
    }

    private Task AuditAsync(DisputeClaim claim, string eventType, string actor, CancellationToken cancellationToken) =>
        audit.AppendAsync(
            new DisputeRegistryAuditEvent(
                Guid.NewGuid(),
                claim.ClaimId,
                claim.Awid,
                claim.TransactionId,
                eventType,
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["status"] = claim.Status.ToString(),
                    ["type"] = claim.Type.ToString(),
                    ["evidenceCount"] = claim.Evidence.Count.ToString(),
                    ["refundDecisionPerformed"] = "false",
                    ["chargebackPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false"
                }),
            cancellationToken);

    private static DisputeClaimResult Map(DisputeClaim claim) => new(
        claim.ClaimId,
        claim.Awid,
        claim.TransactionId,
        claim.Type,
        claim.Reason,
        claim.Amount.AmountMinor,
        claim.Amount.Currency,
        claim.Status,
        claim.SourceChannel,
        claim.Outcome,
        claim.Evidence.Count,
        claim.History.Count,
        claim.CreatedAtUtc,
        claim.UpdatedAtUtc,
        claim.SubmittedAtUtc);
}
