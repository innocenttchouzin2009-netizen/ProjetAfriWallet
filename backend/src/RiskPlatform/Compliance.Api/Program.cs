using Compliance.Application;
using Compliance.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CaseManagementService>();
builder.Services.AddSingleton<InvestigationService>();
builder.Services.AddSingleton<EvidenceService>();
builder.Services.AddSingleton<AssignmentService>();
builder.Services.AddSingleton<EscalationService>();

var app = builder.Build();

app.MapPost("/api/v1/compliance/cases", (CreateCaseRequest request, CaseManagementService service) => Results.Ok(service.CreateCase(request)));
app.MapGet("/api/v1/compliance/cases", (CaseManagementService service) => Results.Ok(service.ListCases()));
app.MapGet("/api/v1/compliance/cases/{caseId}", (Guid caseId, CaseManagementService service) => Results.Ok(service.GetCase(caseId)));
app.MapPut("/api/v1/compliance/cases/{caseId}", (Guid caseId, UpdateCaseRequest request, CaseManagementService service) => Results.Ok(service.UpdateCase(caseId, request)));
app.MapPost("/api/v1/compliance/cases/{caseId}/assign", (Guid caseId, AssignCaseRequest request, CaseManagementService service) => Results.Ok(service.AssignCase(caseId, request)));
app.MapPost("/api/v1/compliance/cases/{caseId}/evidence", (Guid caseId, EvidenceRequest request, CaseManagementService service) => Results.Ok(service.AddEvidence(caseId, request)));
app.MapPost("/api/v1/compliance/cases/{caseId}/decision", (Guid caseId, DecisionRequest request, CaseManagementService service) => Results.Ok(service.AddDecision(caseId, request)));

app.Run();
