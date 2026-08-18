using AfriWallet.Merchants.Onboarding.Domain.Documents;
using AfriWallet.Merchants.Onboarding.Domain.Reviews;

namespace AfriWallet.Merchants.Onboarding.Domain.Cases;

/// Orchestrates sandbox merchant verification only.
/// It never enables payment acceptance, capture, settlement, payout, or ledger mutation.
public sealed class MerchantVerificationCase
{
    private readonly List<VerificationDocument> _documents = new();
    private readonly List<VerificationReviewNote> _notes = new();

    public MerchantVerificationCase(Guid verificationId, string merchantId, string ownerAwid, DateTimeOffset createdAtUtc)
    {
        if (verificationId == Guid.Empty)
            throw new ArgumentException("Verification id is required.", nameof(verificationId));
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(ownerAwid))
            throw new ArgumentException("Owner AWID is required.", nameof(ownerAwid));

        VerificationId = verificationId;
        MerchantId = merchantId.Trim();
        OwnerAwid = ownerAwid.Trim();
        Status = MerchantVerificationStatus.Created;
        Decision = MerchantVerificationDecision.None;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid VerificationId { get; }
    public string MerchantId { get; }
    public string OwnerAwid { get; }
    public MerchantVerificationStatus Status { get; private set; }
    public MerchantVerificationDecision Decision { get; private set; }
    public string? AssignedReviewer { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public IReadOnlyCollection<VerificationDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<VerificationReviewNote> Notes => _notes.AsReadOnly();

    public void AddDocument(
        VerificationDocumentType type,
        string reference,
        string sha256,
        long sizeBytes,
        string contentType,
        string submittedBy,
        DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Document reference is required.", nameof(reference));
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Document SHA-256 is required.", nameof(sha256));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Document content type is required.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(submittedBy))
            throw new ArgumentException("Submitted-by is required.", nameof(submittedBy));

        if (_documents.Any(x => string.Equals(x.Sha256, sha256, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Duplicate verification document hash rejected.");

        _documents.Add(new VerificationDocument(
            Guid.NewGuid(), type, reference.Trim(), sha256.Trim().ToLowerInvariant(), sizeBytes, contentType.Trim(),
            VerificationDocumentStatus.Submitted, submittedBy.Trim(), now));

        if (Status == MerchantVerificationStatus.Created)
            Status = MerchantVerificationStatus.PendingDocuments;

        UpdatedAtUtc = now;
    }

    public void MarkReadyForReview(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status is not (MerchantVerificationStatus.Created or MerchantVerificationStatus.PendingDocuments))
            throw new InvalidOperationException("Verification must be pending documents to become ready for review.");

        Status = MerchantVerificationStatus.ReadyForReview;
        UpdatedAtUtc = now;
    }

    public void AssignReviewer(string reviewer, DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantVerificationStatus.ReadyForReview)
            throw new InvalidOperationException("Verification must be ready for review before assigning a reviewer.");
        if (string.IsNullOrWhiteSpace(reviewer))
            throw new ArgumentException("Reviewer is required.", nameof(reviewer));

        AssignedReviewer = reviewer.Trim();
        Status = MerchantVerificationStatus.UnderReview;
        UpdatedAtUtc = now;
    }

    public void AddNote(string actor, string note, DateTimeOffset now)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note is required.", nameof(note));

        _notes.Add(new VerificationReviewNote(Guid.NewGuid(), actor.Trim(), note.Trim(), now));
        UpdatedAtUtc = now;
    }

    public void RequireManualReview(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantVerificationStatus.UnderReview)
            throw new InvalidOperationException("Verification must be under review.");

        Status = MerchantVerificationStatus.ManualReviewRequired;
        Decision = MerchantVerificationDecision.ManualReviewRequired;
        UpdatedAtUtc = now;
    }

    public void Verify(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantVerificationStatus.UnderReview)
            throw new InvalidOperationException("Verification must be under review.");

        Decision = MerchantVerificationDecision.Verified;
        Status = MerchantVerificationStatus.Verified;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsureMutable();
        if (Status != MerchantVerificationStatus.UnderReview)
            throw new InvalidOperationException("Verification must be under review.");

        Decision = MerchantVerificationDecision.Rejected;
        Status = MerchantVerificationStatus.Rejected;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status is not (MerchantVerificationStatus.Verified or MerchantVerificationStatus.Rejected))
            throw new InvalidOperationException("Only terminal verification decisions can be closed.");

        Status = MerchantVerificationStatus.Closed;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable()
    {
        if (Status is MerchantVerificationStatus.Verified or MerchantVerificationStatus.Rejected or MerchantVerificationStatus.Closed)
            throw new InvalidOperationException("Terminal merchant verification state is immutable.");
    }
}
