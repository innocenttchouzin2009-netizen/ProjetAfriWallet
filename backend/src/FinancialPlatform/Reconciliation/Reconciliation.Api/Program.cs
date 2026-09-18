using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Reconciliation.Api.Review;
using Reconciliation.Api.Resolution;
using Reconciliation.Application.Interfaces;
using Reconciliation.Application.Matching;
using Reconciliation.Application.Review;
using Reconciliation.Application.Resolution;
using Reconciliation.Application.Services;
using Reconciliation.Contracts.Requests;
using Reconciliation.Infrastructure.DataSources;
using Reconciliation.Infrastructure.Repositories;
using Reconciliation.Infrastructure.Resolutions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SandboxReconciliationDataSource>();
builder.Services.AddSingleton<IReconciliationDataSource>(sp => sp.GetRequiredService<SandboxReconciliationDataSource>());
builder.Services.AddSingleton<IReconciliationRepository, InMemoryReconciliationRepository>();

var reconciliationReviewConnectionString =
    builder.Configuration.GetConnectionString("ReconciliationReviewDatabase") ??
    Environment.GetEnvironmentVariable("AFW_RECONCILIATION_REVIEW_DB_CONNECTION_STRING") ??
    "Data Source=reconciliation-review.db";

var reconciliationResolutionConnectionString =
    builder.Configuration.GetConnectionString("ReconciliationResolutionDatabase") ??
    Environment.GetEnvironmentVariable("AFW_RECONCILIATION_RESOLUTION_DB_CONNECTION_STRING") ??
    "Data Source=reconciliation-resolution.db";

builder.Services.AddReconciliationReviewPersistence(reconciliationReviewConnectionString);
builder.Services.AddReconciliationResolutionPersistence(reconciliationResolutionConnectionString);
builder.Services.AddSingleton(new ReconciliationMatcher(TimeSpan.FromMinutes(10)));
builder.Services.AddSingleton<ReconciliationReviewQueueService>();
builder.Services.AddScoped<ReconciliationReviewApplicationService>();
builder.Services.AddScoped<ReconciliationResolutionApplicationService>();
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddOpenApi();

var jwtIssuer = builder.Configuration["Auth:Jwt:Issuer"] ?? "https://identity.afrikawallet.local";
var jwtAudience = builder.Configuration["Auth:Jwt:Audience"] ?? "afrikawallet-reconciliation";
var jwtSigningKey = builder.Configuration["Auth:Jwt:SigningKey"] ??
    Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY") ??
    throw new InvalidOperationException("Auth JWT signing key is not configured for reconciliation review API.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var reviewDb = scope.ServiceProvider.GetRequiredService<ReconciliationReviewDbContext>();
    await reviewDb.Database.EnsureCreatedAsync();

    var resolutionDb = scope.ServiceProvider.GetRequiredService<ReconciliationResolutionDbContext>();
    await resolutionDb.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "afriwallet-reconciliation"
}));

app.MapPost("/api/v1/reconciliation/runs", async (
    StartReconciliationRequest request,
    ReconciliationService service,
    CancellationToken cancellationToken) =>
{
    var run = await service.RunAsync(
        request.PartnerId,
        request.PeriodStartUtc,
        request.PeriodEndUtc,
        cancellationToken);

    return Results.Created($"/api/v1/reconciliation/runs/{run.RunId}", run);
});

app.MapGet("/api/v1/reconciliation/runs/{runId:guid}", async (
    Guid runId,
    IReconciliationRepository repository,
    CancellationToken cancellationToken) =>
{
    var run = await repository.GetRunAsync(runId, cancellationToken);
    return run is null ? Results.NotFound() : Results.Ok(run);
});

app.MapReconciliationReviewEndpoints();
app.MapReconciliationResolutionEndpoints();
app.MapOpenApi();
app.Run();

public partial class Program;
