using RiskScoring.Contracts;

namespace RiskScoring.Application;

public sealed class RiskAggregationService
{
    private readonly RiskWeightService _weightService = new();

    public IReadOnlyList<RiskFactorContribution> Aggregate(RiskEvaluationRequest request)
    {
        var weights = _weightService.GetWeights();
        var contributions = new List<RiskFactorContribution>();

        contributions.Add(new RiskFactorContribution { FactorId = "fraud", Weight = weights["fraud"], Contribution = Math.Min(request.FraudScore, weights["fraud"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "aml", Weight = weights["aml"], Contribution = Math.Min(request.AmlScore, weights["aml"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "device", Weight = weights["device"], Contribution = Math.Min(request.DeviceScore, weights["device"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "account-age", Weight = weights["account-age"], Contribution = request.AccountAgeDays < 30 ? weights["account-age"] : 0 });
        contributions.Add(new RiskFactorContribution { FactorId = "beneficiary-history", Weight = weights["beneficiary-history"], Contribution = Math.Min(request.BeneficiaryHistoryScore, weights["beneficiary-history"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "kyc", Weight = weights["kyc"], Contribution = Math.Min(request.KycProfileScore, weights["kyc"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "geo", Weight = weights["geo"], Contribution = Math.Min(request.GeoScore, weights["geo"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "behaviour", Weight = weights["behaviour"], Contribution = Math.Min(request.BehaviourScore, weights["behaviour"]) });
        contributions.Add(new RiskFactorContribution { FactorId = "payment-type", Weight = weights["payment-type"], Contribution = request.PaymentType == "merchant" ? weights["payment-type"] : 0 });

        return contributions;
    }
}
