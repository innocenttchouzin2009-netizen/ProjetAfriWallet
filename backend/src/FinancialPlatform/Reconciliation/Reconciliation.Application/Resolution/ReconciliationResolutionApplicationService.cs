using Reconciliation.Application.Review;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Resolution;

public sealed class ReconciliationResolutionApplicationService(
    IReconciliationReviewRepository reviewRepository,
    IReconciliationResolutionRepository resolutionRepository)
{
    public async Task<ReconciliationResolutionExecutionResult> ResolveAsync(
        ResolveReconciliationReviewCommand command,
        ReconciliationResolutionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(accessScope);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCommand(command);

        var existing = await resolutionRepository.GetByReviewIdAsync(command.ReviewId, cancellationToken);
        if (existing is not null)
        {
            if (!accessScope.AllowsPartner(existing.PartnerId))
                return ReconciliationResolutionExecutionResult.AccessDenied();

            EnsureEquivalent(existing, command);
            return ReconciliationResolutionExecutionResult.Existing(existing);
        }

        var review = await reviewRepository.GetAsync(command.ReviewId, cancellationToken);
        if (review is null)
            return ReconciliationResolutionExecutionResult.ReviewNotFound();

        if (!accessScope.AllowsPartner(review.PartnerId))
            return ReconciliationResolutionExecutionResult.AccessDenied();

        if (review.Status is not (ReconciliationReviewStatus.Approved or ReconciliationReviewStatus.Rejected))
            return ReconciliationResolutionExecutionResult.ReviewNotFinal();

        if (review.DecidedAtUtc is null)
            throw new InvalidOperationException("A final review must carry a decision timestamp.");

        if (command.ResolvedAtUtc < review.DecidedAtUtc.Value)
            throw new ArgumentException("Resolution time cannot be earlier than review decision time.", nameof(command));

        var resolution = ReconciliationResolution.Create(
            review.ReviewId,
            review.PartnerId,
            review.InternalRecordId,
            review.ExternalRecordId,
            command.Disposition,
            command.Rationale,
            command.EvidenceReference,
            accessScope.ActorId,
            command.ResolvedAtUtc);

        await resolutionRepository.AddAsync(resolution, cancellationToken);
        return ReconciliationResolutionExecutionResult.Created(resolution);
    }

    public async Task<ReconciliationResolutionLookupResult> GetByReviewIdAsync(
        Guid reviewId,
        ReconciliationResolutionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));
        ArgumentNullException.ThrowIfNull(accessScope);
        cancellationToken.ThrowIfCancellationRequested();

        var resolution = await resolutionRepository.GetByReviewIdAsync(reviewId, cancellationToken);
        if (resolution is null)
            return ReconciliationResolutionLookupResult.NotFound();

        return accessScope.AllowsPartner(resolution.PartnerId)
            ? ReconciliationResolutionLookupResult.Found(resolution)
            : ReconciliationResolutionLookupResult.AccessDenied();
    }

    public async Task<ReconciliationResolutionListResult> ListAsync(
        ReconciliationResolutionQuery query,
        ReconciliationResolutionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(accessScope);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateQuery(query);

        var requestedPartner = Normalize(query.PartnerId);
        if (requestedPartner is not null && !accessScope.AllowsPartner(requestedPartner))
            return ReconciliationResolutionListResult.AccessDenied();

        IReadOnlyCollection<string>? partnerIds = null;
        if (!accessScope.CanAccessAllPartners)
        {
            partnerIds = requestedPartner is null
                ? accessScope.PartnerIds.ToArray()
                : new[] { requestedPartner };
        }
        else if (requestedPartner is not null)
        {
            partnerIds = new[] { requestedPartner };
        }

        if (partnerIds is { Count: 0 })
            return ReconciliationResolutionListResult.Success(Array.Empty<ReconciliationResolution>());

        var resolutions = await resolutionRepository.ListAsync(
            new ReconciliationResolutionRepositoryQuery(
                partnerIds,
                query.Disposition,
                Normalize(query.ResolvedBy),
                query.ResolvedFromUtc,
                query.ResolvedToUtc,
                query.Limit),
            cancellationToken);

        return ReconciliationResolutionListResult.Success(resolutions);
    }

    private static void ValidateCommand(ResolveReconciliationReviewCommand command)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(command));
        if (!Enum.IsDefined(command.Disposition))
            throw new ArgumentOutOfRangeException(nameof(command));
        if (string.IsNullOrWhiteSpace(command.Rationale))
            throw new ArgumentException("Resolution rationale is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.EvidenceReference))
            throw new ArgumentException("Corrective evidence reference is required.", nameof(command));
        if (command.ResolvedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Resolution timestamp must be UTC.", nameof(command));
    }

    private static void ValidateQuery(ReconciliationResolutionQuery query)
    {
        if (query.Limit is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(query), "Resolution query limit must be between 1 and 500.");
        if (query.Disposition is not null && !Enum.IsDefined(query.Disposition.Value))
            throw new ArgumentOutOfRangeException(nameof(query), "Unknown reconciliation resolution disposition.");
        if (query.ResolvedFromUtc is not null && query.ResolvedFromUtc.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ResolvedFromUtc must be UTC.", nameof(query));
        if (query.ResolvedToUtc is not null && query.ResolvedToUtc.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ResolvedToUtc must be UTC.", nameof(query));
        if (query.ResolvedFromUtc is not null &&
            query.ResolvedToUtc is not null &&
            query.ResolvedFromUtc > query.ResolvedToUtc)
            throw new ArgumentException("Resolution query start cannot be after end.", nameof(query));
    }

    private static void EnsureEquivalent(
        ReconciliationResolution existing,
        ResolveReconciliationReviewCommand command)
    {
        if (existing.Disposition != command.Disposition ||
            !string.Equals(existing.Rationale, command.Rationale.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existing.EvidenceReference, command.EvidenceReference.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Review already has a different reconciliation resolution.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
