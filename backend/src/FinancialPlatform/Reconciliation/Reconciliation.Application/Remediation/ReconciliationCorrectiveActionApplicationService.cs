using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Remediation;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Remediation;

public sealed class ReconciliationCorrectiveActionApplicationService(
    IReconciliationResolutionRepository resolutionRepository,
    IReconciliationCorrectiveActionRepository correctiveActionRepository)
{
    public async Task<ReconciliationCorrectiveActionExecutionResult> CreateAsync(
        CreateReconciliationCorrectiveActionCommand command,
        ReconciliationResolutionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(accessScope);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCommand(command);

        var resolution = await resolutionRepository.GetByReviewIdAsync(command.ReviewId, cancellationToken);
        if (resolution is null)
            return ReconciliationCorrectiveActionExecutionResult.ResolutionNotFound();

        if (!accessScope.AllowsPartner(resolution.PartnerId))
            return ReconciliationCorrectiveActionExecutionResult.AccessDenied();

        if (resolution.Disposition != ReconciliationResolutionDisposition.RecordCorrected)
            return ReconciliationCorrectiveActionExecutionResult.ResolutionNotEligible();

        if (command.CreatedAtUtc < resolution.ResolvedAtUtc)
            throw new ArgumentException(
                "Corrective action creation time cannot be earlier than resolution time.",
                nameof(command));

        var existing = await correctiveActionRepository.GetByResolutionIdAsync(
            resolution.ResolutionId,
            cancellationToken);

        if (existing is not null)
        {
            EnsureEquivalent(existing, command);
            return ReconciliationCorrectiveActionExecutionResult.Existing(existing);
        }

        var action = ReconciliationCorrectiveAction.Create(
            resolution.ResolutionId,
            resolution.ReviewId,
            resolution.PartnerId,
            resolution.InternalRecordId,
            resolution.ExternalRecordId,
            command.ActionCode,
            command.Description,
            accessScope.ActorId,
            command.CreatedAtUtc);

        await correctiveActionRepository.AddAsync(action, cancellationToken);
        return ReconciliationCorrectiveActionExecutionResult.Created(action);
    }

    public async Task<ReconciliationCorrectiveActionLookupResult> GetByResolutionIdAsync(
        Guid resolutionId,
        ReconciliationResolutionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));
        ArgumentNullException.ThrowIfNull(accessScope);
        cancellationToken.ThrowIfCancellationRequested();

        var action = await correctiveActionRepository.GetByResolutionIdAsync(resolutionId, cancellationToken);
        if (action is null)
            return ReconciliationCorrectiveActionLookupResult.NotFound();

        return accessScope.AllowsPartner(action.PartnerId)
            ? ReconciliationCorrectiveActionLookupResult.Found(action)
            : ReconciliationCorrectiveActionLookupResult.AccessDenied();
    }

    private static void ValidateCommand(CreateReconciliationCorrectiveActionCommand command)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.ActionCode))
            throw new ArgumentException("Corrective action code is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Description))
            throw new ArgumentException("Corrective action description is required.", nameof(command));
        if (command.CreatedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Corrective action creation timestamp must be UTC.", nameof(command));
    }

    private static void EnsureEquivalent(
        ReconciliationCorrectiveAction existing,
        CreateReconciliationCorrectiveActionCommand command)
    {
        if (!string.Equals(existing.ActionCode, command.ActionCode.Trim().ToUpperInvariant(), StringComparison.Ordinal) ||
            !string.Equals(existing.Description, command.Description.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Resolution already has a different reconciliation corrective action.");
        }
    }
}
