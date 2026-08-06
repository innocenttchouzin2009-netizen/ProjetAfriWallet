using AML.Application;
using AML.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MonitoringEngine>();

var app = builder.Build();

app.MapPost("/api/v1/aml/evaluate", (MonitoringEvaluationRequest request, MonitoringEngine engine) =>
{
    var decision = engine.Evaluate(request);
    return Results.Ok(decision);
});

app.MapGet("/api/v1/aml/alerts", () => Results.Ok(new[] { new { id = "alert-001", status = "open" } }));
app.MapGet("/api/v1/aml/cases/{caseId:guid}", (Guid caseId) => Results.Ok(new { caseId, status = "review" }));

app.Run();
