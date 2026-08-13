using BankRouting.Application.Contracts;
using BankRouting.Application.Interfaces;
using BankRouting.Application.Services;
using BankRouting.Infrastructure.Registries;
using BankRouting.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBankRailRegistry, InMemoryBankRailRegistry>();
builder.Services.AddSingleton<IBankRoutingDecisionRepository, InMemoryBankRoutingDecisionRepository>();
builder.Services.AddScoped<BankRoutingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { service = "afriwallet-bank-routing", status = "healthy" }));

app.MapPost("/api/v1/banking/routing/evaluate", async (RoutingRequest request, BankRoutingService service, CancellationToken ct) =>
{
    var decision = await service.EvaluateAsync(request, ct);
    return Results.Ok(decision);
});

app.MapGet("/api/v1/banking/routing/decisions/{decisionId:guid}", async (Guid decisionId, IBankRoutingDecisionRepository repository, CancellationToken ct) =>
{
    var decision = await repository.GetAsync(decisionId, ct);
    return decision is null ? Results.NotFound() : Results.Ok(decision);
});

app.MapOpenApi();
app.Run();

public partial class Program;
