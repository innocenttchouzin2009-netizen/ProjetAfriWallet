using AfriWallet.Compliance.Screening.Api.Contracts;
using AfriWallet.Compliance.Screening.Application.Abstractions;
using AfriWallet.Compliance.Screening.Application.Matching;
using AfriWallet.Compliance.Screening.Application.Screening;
using AfriWallet.Compliance.Screening.Domain.Matching;
using AfriWallet.Compliance.Screening.Domain.Subjects;
using AfriWallet.Compliance.Screening.Infrastructure;
using AfriWallet.Compliance.Screening.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IScreeningListProvider, SandboxSanctionsProvider>();
builder.Services.AddSingleton<IScreeningListProvider, SandboxPepProvider>();
builder.Services.AddSingleton<IScreeningProviderRegistry, ScreeningProviderRegistry>();
builder.Services.AddSingleton<IScreeningResultRepository, InMemoryScreeningResultRepository>();
builder.Services.AddSingleton<IScreeningAuditStore, InMemoryScreeningAuditStore>();
builder.Services.AddSingleton<IScreeningClock, SystemScreeningClock>();
builder.Services.AddSingleton(ScreeningThresholds.Default);
builder.Services.AddSingleton<ScreeningMatcher>();
builder.Services.AddSingleton<ScreeningService>();

var app = builder.Build();

const string Actor = "afriwallet-system";

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy",
        delivery = "AFW-DLV-0016.3",
        sources = "SANDBOX ONLY"
    }));

app.MapPost(
    "/api/v1/compliance/screening",
    async (
        ScreenSubjectRequest request,
        ScreeningService service,
        CancellationToken cancellationToken) =>
    {
        var subject = new ScreeningSubject(
            request.SubjectId,
            request.Type,
            request.FullName,
            request.DateOfBirth,
            request.CountryCode,
            request.ExternalReference);
        var result = await service.ScreenAsync(
            new ScreenSubjectCommand(subject, Actor),
            cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/api/v1/compliance/screening/{subjectId:guid}/matches",
    async (
        Guid subjectId,
        IScreeningResultRepository repository,
        CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetBySubjectAsync(subjectId, cancellationToken)));

app.Run();

public partial class Program;