using AfriWallet.Fraud.Intelligence.Api.Contracts;
using AfriWallet.Fraud.Intelligence.Application.Abstractions;
using AfriWallet.Fraud.Intelligence.Application.Policies;
using AfriWallet.Fraud.Intelligence.Application.Services;
using AfriWallet.Fraud.Intelligence.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFraudIntelligenceSource, SandboxFraudIntelligenceSource>();
builder.Services.AddSingleton<IFraudIntelligenceRepository, InMemoryFraudIntelligenceRepository>();
builder.Services.AddSingleton<IFraudIntelligenceAuditStore, InMemoryFraudIntelligenceAuditStore>();
builder.Services.AddSingleton<IFraudIntelligenceClock, SystemFraudIntelligenceClock>();
builder.Services.AddSingleton<FraudCorrelationPolicy>();
builder.Services.AddSingleton<FraudIntelligenceService>();

var app = builder.Build();
const string actor = "afriwallet-fraud-intelligence-system";

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", delivery = "AFW-DLV-0017.6", correlation = "DETERMINISTIC", machineLearning = false, enforcement = false }));
app.MapPost("/api/v1/fraud/intelligence/correlate", async (CorrelateFraudRequest request, FraudIntelligenceService service, CancellationToken ct) => Results.Ok(await service.CorrelateAsync(new CorrelateFraudCommand(request.Awid, actor), ct)));
app.MapGet("/api/v1/fraud/intelligence/{awid}", async (string awid, IFraudIntelligenceRepository repository, CancellationToken ct) => (await repository.GetLatestAsync(awid, ct)) is { } result ? Results.Ok(result) : Results.NotFound());
app.Run();