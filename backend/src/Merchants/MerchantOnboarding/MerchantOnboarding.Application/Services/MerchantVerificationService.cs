using AfriWallet.Merchants.Onboarding.Application.Abstractions;
using AfriWallet.Merchants.Onboarding.Application.Commands;
using AfriWallet.Merchants.Onboarding.Application.Policies;
using AfriWallet.Merchants.Onboarding.Application.Results;
using AfriWallet.Merchants.Onboarding.Domain.Cases;

namespace AfriWallet.Merchants.Onboarding.Application.Services;

public sealed class MerchantVerificationService(
    IMerchantVerificationRepository repository,
    IMerchantProfileReader profiles,
    IMerchantVerificationProvider provider,
    IMerchantVerificationAuditStore audit,
    IMerchantVerificationClock clock,
    MerchantVerificationPolicy policy)
{
    public async Task<MerchantVerificationResult> CreateAsync(CreateVerificationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.MerchantId))
            throw new ArgumentException("Merchant id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var existing = await repository.GetByMerchantAsync(command.MerchantId, cancellationToken);
        if (existing is not null)
            return Map(existing);

        var profile = await profiles.GetAsync(command.MerchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant not found.");

        var verification = new MerchantVerificationCase(Guid.NewGuid(), profile.MerchantId, profile.OwnerAwid, clock.UtcNow);
        await repository.AddAsync(verification, cancellationToken);
        await AuditAsync(verification, "verification.created", command.Actor, cancellationToken);
        return Map(verification);
    }

    public async Task<MerchantVerificationResult> AddDocumentAsync(AddVerificationDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var verification = await RequireAsync(command.VerificationId, cancellationToken);
        verification.AddDocument(command.Type, command.Reference, command.Sha256, command.SizeBytes, command.ContentType, command.SubmittedBy, clock.UtcNow);

        if (verification.Status == MerchantVerificationStatus.PendingDocuments && policy.HasMinimumDocuments(verification.Documents))
            verification.MarkReadyForReview(clock.UtcNow);

        await repository.SaveAsync(verification, cancellationToken);
        await AuditAsync(verification, "verification.document_added", command.Actor, cancellationToken);
        return Map(verification);
    }

    public async Task<MerchantVerificationResult> AssignReviewerAsync(AssignVerificationReviewerCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var verification = await RequireAsync(command.VerificationId, cancellationToken);
        verification.AssignReviewer(command.Reviewer, clock.UtcNow);
        await repository.SaveAsync(verification, cancellationToken);
        await AuditAsync(verification, "verification.reviewer_assigned", command.Actor, cancellationToken);
        return Map(verification);
    }

    public async Task<MerchantVerificationResult> AddNoteAsync(AddVerificationNoteCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var verification = await RequireAsync(command.VerificationId, cancellationToken);
        verification.AddNote(command.Actor, command.Note, clock.UtcNow);
        await repository.SaveAsync(verification, cancellationToken);
        await AuditAsync(verification, "verification.note_added", command.Actor, cancellationToken);
        return Map(verification);
    }

    public async Task<MerchantVerificationResult> ExecuteAsync(ExecuteVerificationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var verification = await RequireAsync(command.VerificationId, cancellationToken);
        if (verification.Status != MerchantVerificationStatus.UnderReview)
            throw new InvalidOperationException("Verification must be under review before execution.");

        var profile = await profiles.GetAsync(verification.MerchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant not found.");

        var result = await provider.VerifyAsync(
            new MerchantVerificationProviderRequest(verification.VerificationId, verification.MerchantId, verification.OwnerAwid, profile.CountryCode, verification.Documents),
            cancellationToken);

        var eventType = result.Decision switch
        {
            VerificationProviderDecision.Verified => "verification.executed.verified",
            VerificationProviderDecision.Rejected => "verification.executed.rejected",
            _ => "verification.executed.manual_review_required"
        };

        switch (result.Decision)
        {
            case VerificationProviderDecision.Verified:
                verification.Verify(clock.UtcNow);
                break;
            case VerificationProviderDecision.Rejected:
                verification.Reject(clock.UtcNow);
                break;
            default:
                verification.RequireManualReview(clock.UtcNow);
                break;
        }

        await repository.SaveAsync(verification, cancellationToken);
        await AuditAsync(verification, eventType, command.Actor, cancellationToken);
        return Map(verification);
    }

    public async Task<MerchantVerificationResult> GetAsync(Guid verificationId, CancellationToken cancellationToken = default) =>
        Map(await RequireAsync(verificationId, cancellationToken));

    private async Task<MerchantVerificationCase> RequireAsync(Guid verificationId, CancellationToken cancellationToken) =>
        await repository.GetAsync(verificationId, cancellationToken)
        ?? throw new KeyNotFoundException("Merchant verification not found.");

    private async Task AuditAsync(MerchantVerificationCase verification, string eventType, string actor, CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new MerchantVerificationAuditEvent(
                Guid.NewGuid(),
                verification.VerificationId,
                verification.MerchantId,
                eventType,
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["status"] = verification.Status.ToString(),
                    ["decision"] = verification.Decision.ToString(),
                    ["documentCount"] = verification.Documents.Count.ToString(),
                    ["sandboxVerification"] = "true",
                    ["paymentAcceptanceEnabled"] = "false",
                    ["captureEnabled"] = "false",
                    ["settlementEnabled"] = "false",
                    ["payoutEnabled"] = "false",
                    ["moneyMovementPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false"
                }),
            cancellationToken);
    }

    private static MerchantVerificationResult Map(MerchantVerificationCase verification) => new(
        verification.VerificationId,
        verification.MerchantId,
        verification.OwnerAwid,
        verification.Status,
        verification.Decision,
        verification.AssignedReviewer,
        verification.Documents.Count,
        verification.Notes.Count,
        verification.CreatedAtUtc,
        verification.UpdatedAtUtc);
}
