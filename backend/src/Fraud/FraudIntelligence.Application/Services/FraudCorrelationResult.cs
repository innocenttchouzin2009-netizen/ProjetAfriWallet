using AfriWallet.Fraud.Intelligence.Domain.Findings;
using AfriWallet.Fraud.Intelligence.Domain.Patterns;

namespace AfriWallet.Fraud.Intelligence.Application.Services;

public sealed record FraudCorrelationResult(Guid FindingId, string Awid, int CorrelationScore, IntelligenceSeverity Severity, IReadOnlyCollection<FraudPattern> Patterns, DateTimeOffset CreatedAtUtc);