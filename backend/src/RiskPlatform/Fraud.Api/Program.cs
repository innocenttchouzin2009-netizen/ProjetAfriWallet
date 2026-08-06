using Fraud.Application;
using Fraud.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<FraudEngine>();

var app = builder.Build();

app.MapPost("/api/v1/fraud/evaluate", (FraudEvaluationRequest request, FraudEngine engine) =>
{
    var decision = engine.Evaluate(request);
    return Results.Ok(decision);
});

app.MapGet("/api/v1/fraud/rules", () => Results.Ok(new[]
{
    new { id = "amount-threshold", enabled = true, scoreDelta = 35 },
    new { id = "velocity-window", enabled = true, scoreDelta = 20 },
    new { id = "unknown-device", enabled = true, scoreDelta = 25 },
    new { id = "geo-anomaly", enabled = true, scoreDelta = 20 },
    new { id = "new-beneficiary", enabled = true, scoreDelta = 15 },
    new { id = "repeated-failures", enabled = true, scoreDelta = 20 },
    new { id = "merchant-threshold", enabled = true, scoreDelta = 15 }
}));

app.MapGet("/api/v1/fraud/cases/{caseId:guid}", (Guid caseId, FraudEngine engine) =>
{
    var decision = engine.Evaluate(new FraudEvaluationRequest { TransactionId = caseId.ToString(), Timestamp = DateTimeOffset.UtcNow });
    return Results.Ok(new { caseId, decision = decision.Decision.ToString(), decision.Score, decision.RiskLevel });
});

app.Run();
