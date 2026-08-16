using AfriWallet.Compliance.RiskScoring.Api.Contracts;
using AfriWallet.Compliance.RiskScoring.Application.Abstractions;
using AfriWallet.Compliance.RiskScoring.Application.Policies;
using AfriWallet.Compliance.RiskScoring.Application.Scoring;
using AfriWallet.Compliance.RiskScoring.Infrastructure;
using AfriWallet.Compliance.RiskScoring.Infrastructure.Gateways;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IKycRiskSignalProvider, SandboxKycRiskSignalProvider>();
builder.Services.AddSingleton<IScreeningRiskSignalProvider, SandboxScreeningRiskSignalProvider>();
builder.Services.AddSingleton<IAmlRiskSignalProvider, SandboxAmlRiskSignalProvider>();
builder.Services.AddSingleton<IRiskProfileRepository, InMemoryRiskProfileRepository>();
builder.Services.AddSingleton<IRiskAuditStore, InMemoryRiskAuditStore>();
builder.Services.AddSingleton<IRiskClock, SystemRiskClock>();
builder.Services.AddSingleton<RiskScoringPolicy>();
builder.Services.AddSingleton<FinancialRiskScoringService>();

var app = builder.Build();
const string Actor = "afriwallet-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0016.5",
    policy = "SANDBOX"
}));

app.MapPost(
    "/api/v1/compliance/risk/calculate",
    async (
        CalculateRiskRequest request,
        FinancialRiskScoringService service,
        CancellationToken cancellationToken) =>
            Results.Ok(await service.CalculateAsync(
                new CalculateRiskCommand(request.Awid, Actor),
                cancellationToken)));

app.MapGet(
    "/api/v1/compliance/risk/{awid}",
    async (
        string awid,
        IRiskProfileRepository repository,
        CancellationToken cancellationToken) =>
    {
        var result = await repository.GetLatestAsync(awid, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.Run();

public partial class Program;