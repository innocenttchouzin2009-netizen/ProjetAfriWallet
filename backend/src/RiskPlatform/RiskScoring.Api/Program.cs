using RiskScoring.Application;
using RiskScoring.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RiskScoringEngine>();

var app = builder.Build();

app.MapPost("/api/v1/risk/evaluate", (RiskEvaluationRequest request, RiskScoringEngine engine) =>
{
    var result = engine.Evaluate(request);
    return Results.Ok(result);
});

app.MapGet("/api/v1/risk/profiles/{awid}", (string awid) => Results.Ok(new { awid, score = 35 }));
app.MapGet("/api/v1/risk/evaluations/{evaluationId:guid}", (Guid evaluationId) => Results.Ok(new { evaluationId, status = "completed" }));
app.MapGet("/api/v1/risk/factors", () => Results.Ok(new[] { new { id = "fraud", weight = 30 }, new { id = "aml", weight = 25 } }));

app.Run();
