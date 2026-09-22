using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Application.Scoring;
using PaymentRouting.Application.Services;
using PaymentRouting.Contracts.Requests;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Persistence;
using PaymentRouting.Infrastructure.Providers;
using PaymentRouting.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("PaymentRoutingDatabase")
    ?? Environment.GetEnvironmentVariable("AFW_PAYMENT_ROUTING_DB_CONNECTION_STRING")
    ?? "Data Source=payment-routing.db";

builder.Services.AddDbContext<PaymentRoutingDbContext>(
    options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IPaymentProviderRepository, EfPaymentProviderRepository>();
builder.Services.AddScoped<IRoutingDecisionRepository, EfRoutingDecisionRepository>();
builder.Services.AddSingleton<PaymentRouteScorer>();
builder.Services.AddScoped<PaymentRoutingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentRoutingDbContext>();
    await db.Database.EnsureCreatedAsync();

    await SandboxProviderBootstrap.SeedAsync(
        scope.ServiceProvider.GetRequiredService<IPaymentProviderRepository>(),
        CancellationToken.None);
}

app.MapGet(
    "/health/live",
    () => Results.Ok(new
    {
        status = "Healthy",
        service = "afriwallet-payment-routing",
        durablePersistence = true
    }));

app.MapPost(
    "/api/v1/payment-routing/route",
    async (
        RoutePaymentRequest request,
        PaymentRoutingService service,
        CancellationToken cancellationToken) =>
    {
        var decision = await service.RouteAsync(
            new RoutingRequest(
                request.PaymentIntentId,
                request.CountryCode,
                request.CurrencyCode,
                request.AmountMinor,
                request.RequestedRail,
                request.PreferredProviderId,
                request.CorrelationId),
            policy: null,
            cancellationToken);

        return Results.Ok(decision);
    });

app.MapGet(
    "/api/v1/payment-routing/providers",
    async (
        IPaymentProviderRepository repository,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(await repository.ListAsync(cancellationToken));
    });

app.MapGet(
    "/api/v1/payment-routing/decisions/{paymentIntentId:guid}",
    async (
        Guid paymentIntentId,
        IRoutingDecisionRepository repository,
        CancellationToken cancellationToken) =>
    {
        var decision = await repository.GetByPaymentIntentAsync(
            paymentIntentId,
            cancellationToken);

        return decision is null ? Results.NotFound() : Results.Ok(decision);
    });

app.MapOpenApi();
app.Run();

public partial class Program;
