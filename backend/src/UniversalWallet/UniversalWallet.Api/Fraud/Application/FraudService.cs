using UniversalWallet.Api.Fraud.Domain;

namespace UniversalWallet.Api.Fraud.Application;

public sealed record CreateFraudAssessmentRequest(
    Guid PaymentIntentId,
    Guid PayerAwidId,
    Guid SourceWalletId,
    string DeviceId,
    string SessionId,
    int RiskScore,
    string RuleSetVersion,
    string CorrelationId);

public sealed class FraudService
{
    private readonly List<FraudAssessment> _assessments = new();
    private readonly List<FraudReviewCase> _reviews = new();

    public Task<FraudAssessment> CreateAssessmentAsync(CreateFraudAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var assessment = new FraudAssessment
        {
            PaymentIntentId = request.PaymentIntentId,
            PayerAwidId = request.PayerAwidId,
            SourceWalletId = request.SourceWalletId,
            DeviceId = request.DeviceId,
            SessionId = request.SessionId,
            RiskScore = request.RiskScore,
            RiskLevel = MapLevel(request.RiskScore),
            Decision = MapDecision(request.RiskScore),
            RuleSetVersion = request.RuleSetVersion,
            TriggeredRules = request.RiskScore >= 70 ? new[] { "NEW_DEVICE_HIGH_VALUE" } : Array.Empty<string>(),
            RecommendedAction = request.RiskScore >= 80 ? "BLOCK" : request.RiskScore >= 60 ? "REVIEW" : request.RiskScore >= 40 ? "STEP_UP" : "ALLOW",
            CorrelationId = request.CorrelationId
        };

        _assessments.Add(assessment);
        return Task.FromResult(assessment);
    }

    public Task<FraudReviewCase> CreateReviewCaseAsync(Guid assessmentId, string reasonCode, string assignedTo, CancellationToken cancellationToken = default)
    {
        var assessment = _assessments.FirstOrDefault(item => item.Id == assessmentId);
        if (assessment is null)
        {
            throw new InvalidOperationException("FRAUD_ASSESSMENT_NOT_FOUND");
        }

        var review = new FraudReviewCase
        {
            AssessmentId = assessment.Id,
            PaymentIntentId = assessment.PaymentIntentId,
            Status = FraudReviewStatus.Open,
            Priority = assessment.RiskLevel == FraudRiskLevel.Critical ? "HIGH" : "MEDIUM",
            AssignedTo = assignedTo,
            ReasonCodes = new[] { reasonCode },
            Decision = assessment.Decision == FraudDecision.Block ? FraudDecision.Block : FraudDecision.Review
        };

        _reviews.Add(review);
        return Task.FromResult(review);
    }

    public Task<IReadOnlyList<FraudAssessment>> ListAssessmentsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<FraudAssessment>>(_assessments);

    private static FraudDecision MapDecision(int score) => score switch
    {
        >= 80 => FraudDecision.Block,
        >= 60 => FraudDecision.Review,
        >= 40 => FraudDecision.StepUp,
        _ => FraudDecision.Allow
    };

    private static FraudRiskLevel MapLevel(int score) => score switch
    {
        >= 80 => FraudRiskLevel.Critical,
        >= 60 => FraudRiskLevel.High,
        >= 40 => FraudRiskLevel.Medium,
        _ => FraudRiskLevel.Low
    };
}
