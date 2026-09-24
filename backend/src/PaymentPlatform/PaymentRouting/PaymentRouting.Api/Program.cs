using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Application.Scoring;
using PaymentRouting.Application.Services;
using PaymentRouting.Contracts.Requests;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Providers;
using PaymentRouting.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPaymentProviderRepository, InMemoryPaymentProviderRepository>();

var routingDbPath = builder.Configuration["PaymentRouting:DatabasePath"]
    ?? Path.Combine(AppContext.BaseDirectory, "payment-routing.db");
builder.Services.AddPooledDbContextFactory<PaymentRoutingDbContext>(options =>
    options.UseSqlite($"Data Source={routingDbPath}"));
builder.Services.AddSingleton<IRoutingDecisionRepository, DurableRoutingDecisionRepository>();

builder.Services.AddSingleton<PaymentRouteScorer>();
builder.Services.AddScoped<PaymentRoutingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var db = await app.Services.GetRequiredService<IDbContextFactory<PaymentRoutingDbContext>>().CreateDbContextAsync())
{
    await db.Database.EnsureCreatedAsync();
}

await SandboxProviderBootstrap.SeedAsync(
    app.Services.GetRequiredService<IPaymentProviderRepository>(),
    CancellationToken.None);

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "afriwallet-payment-routing" }));

app.MapPost("/api/v1/payment-routing/route", async (RoutePaymentRequest request, PaymentRoutingService service, CancellationToken cancellationToken) =>
{
    var decision = await service.RouteAsync(
        new RoutingRequest(request.PaymentIntentId, request.CountryCode, request.CurrencyCode, request.AmountMinor, request.RequestedRail, request.PreferredProviderId, request.CorrelationId),
        policy: null,
        cancellationToken);
    return Results.Ok(decision);
});

app.MapGet("/api/v1/payment-routing/providers", async (IPaymentProviderRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)));

app.MapGet("/api/v1/payment-routing/decisions/{paymentIntentId:guid}", async (Guid paymentIntentId, IRoutingDecisionRepository repository, CancellationToken cancellationToken) =>
{
    var decision = await repository.GetByPaymentIntentAsync(paymentIntentId, cancellationToken);
    return decision is null ? Results.NotFound() : Results.Ok(decision);
});

app.MapOpenApi();
app.Run();

public partial class Program;
