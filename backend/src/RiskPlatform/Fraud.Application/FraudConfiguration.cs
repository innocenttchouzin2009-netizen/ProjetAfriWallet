namespace Fraud.Application;

public sealed class FraudConfiguration
{
    public int ReviewScoreThreshold { get; set; } = 30;
    public int BlockScoreThreshold { get; set; } = 80;

    public Dictionary<string, int> RuleScores { get; set; } = new()
    {
        ["amount-threshold"] = 80,
        ["velocity-window"] = 35,
        ["unknown-device"] = 35,
        ["geo-anomaly"] = 35,
        ["new-beneficiary"] = 20,
        ["repeated-failures"] = 35,
        ["merchant-threshold"] = 35
    };

    public Dictionary<string, bool> RuleEnabledStates { get; set; } = new()
    {
        ["amount-threshold"] = true,
        ["velocity-window"] = true,
        ["unknown-device"] = true,
        ["geo-anomaly"] = true,
        ["new-beneficiary"] = true,
        ["repeated-failures"] = true,
        ["merchant-threshold"] = true
    };
}
