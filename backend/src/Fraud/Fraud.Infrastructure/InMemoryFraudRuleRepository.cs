using Fraud.Api.Application;
using Fraud.Api.Domain;

namespace Fraud.Api.Infrastructure;

public sealed class InMemoryFraudRuleRepository : IFraudRuleRepository
{
    private readonly List<FraudRule> _rules = new();

    public FraudRule Add(FraudRule rule)
    {
        _rules.Add(rule);
        return rule;
    }

    public IReadOnlyList<FraudRule> List() => _rules.AsReadOnly();
}
