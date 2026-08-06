namespace RiskScoring.Application;

public sealed class RiskWeightService
{
    public Dictionary<string, int> GetWeights() => new()
    {
        ["fraud"] = 30,
        ["aml"] = 25,
        ["device"] = 15,
        ["account-age"] = 10,
        ["beneficiary-history"] = 10,
        ["kyc"] = 10,
        ["geo"] = 10,
        ["behaviour"] = 10,
        ["payment-type"] = 10
    };
}
