namespace AfriWallet.Merchants.Onboarding.Domain.Reviews;

public sealed record VerificationReviewNote(Guid NoteId, string Actor, string Note, DateTimeOffset CreatedAtUtc);
