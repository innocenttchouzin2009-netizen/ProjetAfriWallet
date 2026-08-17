namespace AfriWallet.Fraud.Investigation.Domain.Notes;

public sealed record FraudInvestigationNote(Guid NoteId, string Author, string Content, DateTimeOffset CreatedAtUtc);