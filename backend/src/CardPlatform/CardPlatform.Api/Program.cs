using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICardProgramRepository, InMemoryCardProgramRepository>();
builder.Services.AddSingleton<CardProgramService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapGet("/api/v1/card-programs", async (CardProgramService service, CancellationToken cancellationToken) =>
{
    var programs = await service.GetAllAsync(cancellationToken);
    return Results.Ok(programs);
});

app.MapGet("/api/v1/card-programs/{programId}", async (string programId, CardProgramService service, CancellationToken cancellationToken) =>
{
    var program = await service.GetByIdAsync(programId, cancellationToken);
    return program is null ? Results.NotFound() : Results.Ok(program);
});

app.MapPost("/internal/card-programs", async (CardProgram request, CardProgramService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/card-programs/{created.ProgramId}", created);
});

app.MapPut("/internal/card-programs/{programId}", async (string programId, CardProgram request, CardProgramService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateAsync(programId, request, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.Run();
