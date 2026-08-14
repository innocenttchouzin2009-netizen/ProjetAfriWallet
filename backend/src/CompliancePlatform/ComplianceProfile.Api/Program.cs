using AfriWallet.CompliancePlatform.ComplianceProfile.Application;
using AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;
using AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Audit;
using AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IComplianceProfileRepository, InMemoryComplianceProfileRepository>();
builder.Services.AddSingleton<IComplianceAuditSink, ComplianceAuditSink>();
builder.Services.AddScoped<ComplianceProfileService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
    service = "afriwallet-compliance-profile",
    status = "healthy"
}));

app.MapPost("/api/v1/compliance/profiles", async (
    CreateComplianceProfileRequest request,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    var profile = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/compliance/profiles/{profile.ProfileId}", profile);
});

app.MapGet("/api/v1/compliance/profiles/{profileId:guid}", async (
    Guid profileId,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    var profile = await service.GetAsync(profileId, cancellationToken);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapGet("/api/v1/compliance/customers/{customerId}/profiles", async (
    string customerId,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await service.ListByCustomerAsync(customerId, cancellationToken));
});

app.MapPost("/api/v1/compliance/profiles/{profileId:guid}/documents", async (
    Guid profileId,
    AddDocumentRequest request,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    if (request.ProfileId != profileId)
    {
        return Results.BadRequest(new { error = "profile_mismatch" });
    }

    return Results.Ok(await service.AddDocumentAsync(request, cancellationToken));
});

app.MapPost("/api/v1/compliance/profiles/{profileId:guid}/review", async (
    Guid profileId,
    ReviewComplianceProfileRequest request,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    if (request.ProfileId != profileId)
    {
        return Results.BadRequest(new { error = "profile_mismatch" });
    }

    return Results.Ok(await service.ReviewAsync(request, cancellationToken));
});

app.MapPost("/api/v1/compliance/profiles/{profileId:guid}/suspend", async (
    Guid profileId,
    string reviewer,
    string reason,
    ComplianceProfileService service,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await service.SuspendAsync(profileId, reviewer, reason, cancellationToken));
});

app.Run();

public partial class Program;
