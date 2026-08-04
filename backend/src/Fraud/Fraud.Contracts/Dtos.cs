namespace Fraud.Api.Contracts;

public sealed record CreateFraudRuleRequest(string Name, string Description, string Severity, string Condition);
public sealed record FraudRuleResponse(Guid RuleId, string Name, string Description, string Severity, string Condition, DateTimeOffset CreatedAt);
