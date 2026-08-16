using AfriWallet.Compliance.IdentityVerification.Api.Contracts;
using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;
using AfriWallet.Compliance.IdentityVerification.Application.Sessions;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;
using AfriWallet.Compliance.IdentityVerification.Infrastructure;
using AfriWallet.Compliance.IdentityVerification.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

var documentProvider = new SandboxVerificationProvider("SANDBOX_DOC", "Sandbox Document Provider", VerificationType.Document);
var selfieProvider = new SandboxVerificationProvider("SANDBOX_SELFIE", "Sandbox Selfie Provider", VerificationType.Selfie);
var livenessProvider = new SandboxVerificationProvider("SANDBOX_LIVENESS", "Sandbox Liveness Provider", VerificationType.Liveness);

builder.Services.AddSingleton<IVerificationSessionRepository, InMemoryVerificationSessionRepository>();
builder.Services.AddSingleton<IVerificationAuditStore, InMemoryVerificationAuditStore>();
builder.Services.AddSingleton<IVerificationClock, SystemVerificationClock>();
builder.Services.AddSingleton<IVerificationProviderRegistry>(new VerificationProviderRegistry(new IVerificationProvider[]
{
    documentProvider,
    selfieProvider,
    livenessProvider
}));
builder.Services.AddScoped<IdentityVerificationService>();

var app = builder.Build();

const string Actor = "identity-verification-api";

app.MapGet("/health/live", () => Results.Ok(new { service = "afriwallet-identity-verification", status = "healthy" }));

app.MapPost("/api/v1/identity-verification/sessions", async (
    CreateVerificationRequest request,
    IdentityVerificationService service,
    CancellationToken ct) =>
{
    var result = await service.CreateAsync(
        new CreateVerificationCommand(
            request.ComplianceProfileId,
            request.Type,
            request.ProviderCode,
            request.IdempotencyKey,
            Actor),
        ct);

    return Results.Created($"/api/v1/identity-verification/sessions/{result.Id}", result);
});

app.MapGet("/api/v1/identity-verification/sessions/{id:guid}", async (
    Guid id,
    IdentityVerificationService service,
    CancellationToken ct) =>
{
    return Results.Ok(await service.GetAsync(id, ct));
});

app.MapPost("/api/v1/identity-verification/sessions/{id:guid}/submit", async (
    Guid id,
    IdentityVerificationService service,
    CancellationToken ct) =>
{
    return Results.Ok(await service.SubmitAsync(id, Actor, ct));
});

app.MapPost("/api/v1/identity-verification/sessions/{id:guid}/processing", async (
    Guid id,
    IdentityVerificationService service,
    CancellationToken ct) =>
{
    return Results.Ok(await service.StartProcessingAsync(id, Actor, ct));
});

app.MapPost("/api/v1/identity-verification/sessions/{id:guid}/complete", async (
    Guid id,
    CompleteVerificationRequest request,
    IdentityVerificationService service,
    CancellationToken ct) =>
{
    return Results.Ok(await service.CompleteAsync(
        new CompleteVerificationCommand(
            id,
            request.Verified,
            request.Code,
            request.ProviderReference,
            Actor),
        ct));
});

app.Run();

public partial class Program;
