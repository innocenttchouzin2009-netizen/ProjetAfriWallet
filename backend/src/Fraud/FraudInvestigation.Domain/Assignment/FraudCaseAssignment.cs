namespace AfriWallet.Fraud.Investigation.Domain.Assignment;

public sealed record FraudCaseAssignment(string AnalystId, string AssignedBy, DateTimeOffset AssignedAtUtc);