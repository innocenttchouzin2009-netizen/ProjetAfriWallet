using Fraud.Api.Domain;

namespace Fraud.Api.Application;

public interface IFraudRuleRepository
{
    FraudRule Add(FraudRule rule);
    IReadOnlyList<FraudRule> List();
}

public sealed record CreateFraudRuleCommand
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public FraudSeverity Severity { get; init; }
    public string Condition { get; init; } = string.Empty;
}

public sealed class FraudRuleCatalogService
{
    private readonly IFraudRuleRepository _repository;

    public FraudRuleCatalogService(IFraudRuleRepository repository)
    {
        _repository = repository;
    }

    public FraudRule Create(CreateFraudRuleCommand command)
    {
        var rule = new FraudRule
        {
            RuleId = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            Severity = command.Severity,
            Condition = command.Condition
        };

        return _repository.Add(rule);
    }

    public IReadOnlyList<FraudRule> List() => _repository.List();

    public FraudEvaluation Evaluate(FraudEvent fraudEvent)
    {
        var matchingRule = _repository.List().FirstOrDefault(rule => rule.Name.Equals(fraudEvent.RuleName, StringComparison.OrdinalIgnoreCase));
        if (matchingRule is null)
        {
            return new FraudEvaluation { IsFlagged = false };
        }

        var amountThreshold = matchingRule.Condition.Contains("amount", StringComparison.OrdinalIgnoreCase) && matchingRule.Condition.Contains(">", StringComparison.Ordinal)
            ? decimal.Parse(matchingRule.Condition.Split('>')[1].Trim())
            : 0m;

        var thresholdReached = fraudEvent.Amount > amountThreshold;

        return new FraudEvaluation
        {
            IsFlagged = thresholdReached,
            RuleName = matchingRule.Name,
            Severity = matchingRule.Severity
        };
    }
}
