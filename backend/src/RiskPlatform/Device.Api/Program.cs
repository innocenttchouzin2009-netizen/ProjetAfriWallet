using Device.Application;
using Device.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DeviceEngine>();

var app = builder.Build();

app.MapPost("/api/v1/device/evaluate", (DeviceEvaluationRequest request, DeviceEngine engine) =>
{
    var result = engine.Evaluate(request);
    return Results.Ok(result);
});

app.MapGet("/api/v1/device/{deviceId}", (string deviceId) => Results.Ok(new { deviceId, status = "known" }));
app.MapGet("/api/v1/device/{deviceId}/history", (string deviceId) => Results.Ok(new[] { new { deviceId, @event = "login" } }));
app.MapPost("/api/v1/device/{deviceId}/trust", (string deviceId) => Results.Ok(new { deviceId, trust = true }));
app.MapPost("/api/v1/device/{deviceId}/revoke", (string deviceId) => Results.Ok(new { deviceId, revoked = true }));

app.Run();
