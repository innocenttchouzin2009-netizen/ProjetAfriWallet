using Fraud.Contracts;

namespace Fraud.Application;

public sealed class FraudRuleEngine
{
    private readonly FraudConfiguration _configuration = new();
    private readonly IReadOnlyList<FraudRule> _rules = new List<FraudRule>
    {
        new("amount-threshold", 35, request => request.Amount > 10000m),
        new("velocity-window", 20, request => request.Velocity >= 4),
        new("unknown-device", 25, request => IsUnknownDevice(request)),
        new("geo-anomaly", 20, request => IsGeoAnomaly(request)),
        new("new-beneficiary", 15, request => IsNewBeneficiary(request)),
        new("repeated-failures", 20, request => GetHistoricalInt(request, "priorFailures") >= 3),
        new("merchant-threshold", 15, request => IsMerchantThresholdExceeded(request))
    };

    public IReadOnlyList<FraudRuleEvaluationResult> Evaluate(FraudEvaluationRequest request)
    {
        var results = new List<FraudRuleEvaluationResult>();
        foreach (var rule in _rules)
        {
            if (!_configuration.RuleEnabledStates.GetValueOrDefault(rule.RuleId, true))
            {
                continue;
            }

            if (rule.Predicate(request))
            {
                var scoreDelta = _configuration.RuleScores.GetValueOrDefault(rule.RuleId, rule.ScoreDelta);
                results.Add(new FraudRuleEvaluationResult(rule.RuleId, scoreDelta));
            }
        }

        return results;
    }

    private static bool IsUnknownDevice(FraudEvaluationRequest request)
    {
        return request.DeviceId.Contains("unknown", StringComparison.OrdinalIgnoreCase) ||
               !GetHistoricalBool(request, "knownDevice");
    }

    private static bool IsGeoAnomaly(FraudEvaluationRequest request)
    {
        return GetHistoricalBool(request, "countryChanged") ||
               request.Country != "CI";
    }

    private static bool IsNewBeneficiary(FraudEvaluationRequest request)
    {
        return GetHistoricalInt(request, "beneficiaryAgeDays") <= 30;
    }

    private static bool IsMerchantThresholdExceeded(FraudEvaluationRequest request)
    {
        return GetHistoricalBool(request, "merchantThresholdExceeded") || request.MerchantId == "merch-999";
    }

    private static bool GetHistoricalBool(FraudEvaluationRequest request, string key)
    {
        if (request.HistoricalBehaviour.TryGetValue(key, out var value))
        {
            return value is bool b && b;
        }

        return false;
    }

    private static int GetHistoricalInt(FraudEvaluationRequest request, string key)
    {
        if (request.HistoricalBehaviour.TryGetValue(key, out var value))
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                short s => s,
                string s => int.TryParse(s, out var parsed) ? parsed : 0,
                _ => 0
            };
        }

        return 0;
    }
}

public sealed record FraudRule(string RuleId, int ScoreDelta, Func<FraudEvaluationRequest, bool> Predicate);
public sealed record FraudRuleEvaluationResult(string RuleId, int ScoreDelta);
