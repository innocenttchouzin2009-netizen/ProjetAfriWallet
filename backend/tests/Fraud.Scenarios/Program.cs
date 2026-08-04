using Fraud.Api.Application;
using Fraud.Api.Domain;
using Fraud.Api.Infrastructure;

var repository = new InMemoryFraudRuleRepository();
var service = new FraudRuleCatalogService(repository);

var failures = new List<string>();

Run("creates rule", () =>
{
    var result = service.Create(new CreateFraudRuleCommand
    {
        Name = "velocity-limit",
        Description = "Blocks suspicious velocity",
        Severity = FraudSeverity.High,
        Condition = "amount > 1000"
    });

    Assert(result.RuleId != Guid.Empty, "rule id should be generated");
    Assert(service.List().Count == 1, "one rule should be available");
});

Run("detects suspicious event", () =>
{
    var rule = service.Create(new CreateFraudRuleCommand
    {
        Name = "location-mismatch",
        Description = "Blocks impossible travel",
        Severity = FraudSeverity.Critical,
        Condition = "distance > 500"
    });

    var evaluation = service.Evaluate(new FraudEvent("location-mismatch", "wallet-1", 600m, "NGN"));

    Assert(evaluation.IsFlagged, "suspicious event should be flagged");
    Assert(evaluation.RuleName == "location-mismatch", "rule name should be returned");
    Assert(evaluation.Severity == FraudSeverity.Critical, "severity should be propagated");
});

Run("allows normal event", () =>
{
    var evaluation = service.Evaluate(new FraudEvent("allow-list", "wallet-2", 200m, "USD"));

    Assert(!evaluation.IsFlagged, "normal event should not be flagged");
    Assert(evaluation.RuleName is null, "no rule name for a clean event");
});

Run("list and search", () =>
{
    var rules = service.List();
    Assert(rules.Count >= 2, "rules should be listed");
    Assert(rules.Any(rule => rule.Name == "velocity-limit"), "velocity-limit should be present");
    Assert(rules.Any(rule => rule.Name == "location-mismatch"), "location-mismatch should be present");
});

if (failures.Count > 0)
{
    Console.WriteLine("Fraud scenarios failed:");
    foreach (var failure in failures)
    {
        Console.WriteLine($"[FAIL] {failure}");
    }
    Environment.Exit(1);
}

Console.WriteLine("All AFW-0005.8.1 fraud engine scenarios passed.");

void Run(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
