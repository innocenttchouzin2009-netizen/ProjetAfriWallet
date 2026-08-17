namespace AfriWallet.Fraud.TransactionFraud.Domain.Signals;

public sealed record FraudSignalSnapshot(
    string EventId,
    string Type,
    string SubjectType,
    string SubjectId,
    string Severity,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Attributes);
