using Operations.Application;
using Operations.Contracts;
using Operations.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OperationsCenterStore>();
builder.Services.AddSingleton<OperationsCenterAuthorizationService>();
builder.Services.AddSingleton<OperationsHealthAggregatorService>();
builder.Services.AddSingleton<OperationsCenterService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "operations-api" }));

app.MapGet("/api/v1/operations/health", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetHealth(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/dashboard", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetDashboard(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/incidents", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetIncidents(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/incidents", (CreateIncidentRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.CreateIncident(request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/incidents/{id:guid}/ack", (Guid id, AcknowledgeIncidentRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.AcknowledgeIncident(id, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/incidents/{id:guid}/resolve", (Guid id, ResolveIncidentRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.ResolveIncident(id, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/alerts", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetAlerts(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/maintenance", (MaintenanceWindowRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.ScheduleMaintenance(request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/deployments", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetDeployments(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/backups", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetBackups(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/dr", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsCenterService service) =>
{
    return Results.Ok(service.GetDisasterRecovery(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.Run();
