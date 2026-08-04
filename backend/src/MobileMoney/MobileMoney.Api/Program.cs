using MobileMoney.Production.Diagnostics;
using MobileMoney.Production.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMtnMomoProductionConfiguration(builder.Configuration);

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/internal/configuration/diagnostics", (ConfigurationDiagnosticService diagnostics) =>
{
    return Results.Ok(diagnostics.GetDiagnosticSnapshot());
});

app.Run();
