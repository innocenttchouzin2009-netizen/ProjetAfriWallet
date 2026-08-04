using Fraud.Api.Application;
using Fraud.Api.Contracts;
using Fraud.Api.Domain;
using Fraud.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFraudRuleRepository, InMemoryFraudRuleRepository>();
builder.Services.AddSingleton<FraudRuleCatalogService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/v1/fraud/rules", (CreateFraudRuleRequest request, FraudRuleCatalogService service) =>
{
    var rule = service.Create(new CreateFraudRuleCommand
    {
        Name = request.Name,
        Description = request.Description,
        Severity = Enum.Parse<FraudSeverity>(request.Severity, ignoreCase: true),
        Condition = request.Condition
    });

    return Results.Created($"/api/v1/fraud/rules/{rule.RuleId}", new FraudRuleResponse(rule.RuleId, rule.Name, rule.Description, rule.Severity.ToString(), rule.Condition, rule.CreatedAt));
});

app.MapGet("/api/v1/fraud/rules", (FraudRuleCatalogService service) =>
{
    var rules = service.List().Select(rule => new FraudRuleResponse(rule.RuleId, rule.Name, rule.Description, rule.Severity.ToString(), rule.Condition, rule.CreatedAt));
    return Results.Ok(new { items = rules });
});

app.Run();
