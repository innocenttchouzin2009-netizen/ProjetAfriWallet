var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/auth/register", () => Results.Ok(new { userId = Guid.NewGuid().ToString(), status = "PENDING" }));

app.Run();
