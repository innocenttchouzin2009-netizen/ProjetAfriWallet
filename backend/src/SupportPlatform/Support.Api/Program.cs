using Support.Application;
using Support.Contracts;
using Support.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemorySupportStore>();
builder.Services.AddSingleton<AssignmentService>();
builder.Services.AddSingleton<SlaService>();
builder.Services.AddSingleton<EscalationService>();
builder.Services.AddSingleton<SupportTimelineService>();
builder.Services.AddSingleton<SupportNotificationService>();
builder.Services.AddSingleton<SupportSearchService>();
builder.Services.AddSingleton<SupportCaseService>();

var app = builder.Build();

app.MapPost("/api/v1/support/cases", (CreateSupportCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.CreateCase(request));
});

app.MapGet("/api/v1/support/cases", (
    string? status,
    string? priority,
    string? category,
    string? assignedTeam,
    SupportCaseService service) =>
{
    var query = new SupportCaseQuery
    {
        Status = status,
        Priority = priority,
        Category = category,
        AssignedTeam = assignedTeam
    };
    return Results.Ok(service.ListCases(query));
});

app.MapGet("/api/v1/support/cases/{caseId:guid}", (Guid caseId, SupportCaseService service) =>
{
    return Results.Ok(service.GetCase(caseId));
});

app.MapPut("/api/v1/support/cases/{caseId:guid}", (Guid caseId, UpdateSupportCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.UpdateCase(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/assign", (Guid caseId, AssignCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.AssignCase(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/messages", (Guid caseId, AddSupportMessageRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.AddMessage(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/notes", (Guid caseId, AddSupportNoteRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.AddInternalNote(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/attachments", (Guid caseId, AddSupportAttachmentRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.AddAttachment(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/escalate", (Guid caseId, EscalateCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.EscalateCase(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/resolve", (Guid caseId, ResolveCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.ResolveCase(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/close", (Guid caseId, CloseCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.CloseCase(caseId, request));
});

app.MapPost("/api/v1/support/cases/{caseId:guid}/reopen", (Guid caseId, ReopenCaseRequest request, SupportCaseService service) =>
{
    return Results.Ok(service.ReopenCase(caseId, request));
});

app.MapGet("/api/v1/support/cases/{caseId:guid}/timeline", (Guid caseId, SupportCaseService service) =>
{
    return Results.Ok(service.GetTimeline(caseId));
});

app.MapGet("/api/v1/support/cases/{caseId:guid}/sla", (Guid caseId, SupportCaseService service) =>
{
    return Results.Ok(service.GetSla(caseId));
});

app.Run();
