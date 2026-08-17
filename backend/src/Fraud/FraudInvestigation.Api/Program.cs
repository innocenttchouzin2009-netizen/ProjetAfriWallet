using AfriWallet.Fraud.Investigation.Api.Contracts;
using AfriWallet.Fraud.Investigation.Application.Abstractions;
using AfriWallet.Fraud.Investigation.Application.Cases;
using AfriWallet.Fraud.Investigation.Application.Policies;
using AfriWallet.Fraud.Investigation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFraudCaseRepository, InMemoryFraudCaseRepository>();
builder.Services.AddSingleton<IFraudInvestigationAuditStore, InMemoryFraudInvestigationAuditStore>();
builder.Services.AddSingleton<IFraudDecisionEvidenceReader, SandboxFraudDecisionEvidenceReader>();
builder.Services.AddSingleton<IFraudInvestigationClock, SystemFraudInvestigationClock>();
builder.Services.AddSingleton<FraudResponsePolicy>();
builder.Services.AddSingleton<FraudInvestigationService>();

var app = builder.Build();
const string actor = "afriwallet-fraud-investigation";

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", delivery = "AFW-DLV-0017.5", execution = "RECOMMENDATION ONLY" }));
app.MapPost("/api/v1/fraud/cases", async (CreateFraudCaseRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.CreateAsync(new CreateFraudCaseCommand(request.Awid, request.TransactionId, request.Title, request.Priority, actor), ct)));
app.MapGet("/api/v1/fraud/cases/{caseId:guid}", async (Guid caseId, IFraudCaseRepository repository, CancellationToken ct) => (await repository.GetAsync(caseId, ct)) is { } result ? Results.Ok(result) : Results.NotFound());
app.MapGet("/api/v1/fraud/cases/by-awid/{awid}", async (string awid, IFraudCaseRepository repository, CancellationToken ct) => Results.Ok(await repository.GetByAwidAsync(awid, ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/assign", async (Guid caseId, AssignFraudCaseRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.AssignAsync(new AssignFraudCaseCommand(caseId, request.AnalystId, actor), ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/investigate", async (Guid caseId, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.StartInvestigationAsync(caseId, actor, ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/notes", async (Guid caseId, AddFraudNoteRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.AddNoteAsync(new AddFraudCaseNoteCommand(caseId, request.Content, actor), ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/escalate", async (Guid caseId, EscalateFraudCaseRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.EscalateAsync(new EscalateFraudCaseCommand(caseId, request.Priority, actor), ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/responses", async (Guid caseId, AddFraudResponseRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.AddResponseAsync(new AddFraudResponseCommand(caseId, request.ResponseType, request.Reason, actor), ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/resolve", async (Guid caseId, ResolveFraudCaseRequest request, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.ResolveAsync(new ResolveFraudCaseCommand(caseId, request.Resolution, actor), ct)));
app.MapPost("/api/v1/fraud/cases/{caseId:guid}/close", async (Guid caseId, FraudInvestigationService service, CancellationToken ct) => Results.Ok(await service.CloseAsync(caseId, actor, ct)));
app.Run();