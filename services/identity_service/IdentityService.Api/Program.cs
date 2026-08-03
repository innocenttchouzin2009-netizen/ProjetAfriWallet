var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/auth/register", () => Results.Ok(new { userId = Guid.NewGuid().ToString(), status = "PENDING" }));

app.Run();
