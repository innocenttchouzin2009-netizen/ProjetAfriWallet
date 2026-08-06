using Operations.Application;
using Operations.Contracts;
using Operations.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryOperationsStore>();
builder.Services.AddSingleton<OperationsAuthorizationService>();
builder.Services.AddSingleton<OperationsPortalService>();

var app = builder.Build();

app.MapGet("/api/v1/operations/dashboard", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.GetDashboard(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/search", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, [AsParameters] OperationsSearchRequest request, OperationsPortalService service) =>
{
    return Results.Ok(service.Search(request, new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/users/{awid}", (string awid, string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.GetUser(awid, new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/transactions/{transactionId}", (string transactionId, string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.GetTransaction(transactionId, new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/services/health", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.GetHealth(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapGet("/api/v1/operations/audit", (string role, string actorId, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.GetAudit(new OperationsContextRequest
    {
        Role = role,
        ActorId = actorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/wallets/{walletId}/suspend", (string walletId, SuspendWalletRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.SuspendWallet(walletId, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/cards/{cardId}/freeze", (string cardId, FreezeCardRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.FreezeCard(cardId, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/cases/{caseId}/assign", (string caseId, AssignCaseRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.AssignCase(caseId, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.MapPost("/api/v1/operations/transactions/{transactionId}/retry", (string transactionId, RetryTransactionRequest request, string role, bool hasMfa, bool hasDeviceTrust, OperationsPortalService service) =>
{
    return Results.Ok(service.RetryTransaction(transactionId, request, new OperationsContextRequest
    {
        Role = role,
        ActorId = request.ActorId,
        HasMfa = hasMfa,
        HasDeviceTrust = hasDeviceTrust
    }));
});

app.Run();
