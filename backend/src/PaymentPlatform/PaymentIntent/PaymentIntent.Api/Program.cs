using PaymentIntent.Application.Interfaces;
using PaymentIntent.Application.Services;
using PaymentIntent.Contracts.Requests;
using PaymentIntent.Infrastructure.Repositories;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IPaymentIntentRepository,
    InMemoryPaymentIntentRepository>();

builder.Services.AddScoped<
    PaymentIntentService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet(
    "/health/live",
    () => Results.Ok(new
    {
        status = "Healthy",
        service = "afriwallet-payment-intent"
    }));

app.MapPost(
    "/api/v1/payment-intents",
    async (
        CreatePaymentIntentRequest request,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        var intent =
            await service.CreateAsync(
                request.Reference,
                request.PayerId,
                request.PayeeId,
                request.AmountMinor,
                request.CurrencyCode,
                request.PaymentMethod,
                request.IdempotencyKey,
                TimeSpan.FromMinutes(
                    request.LifetimeMinutes),
                cancellationToken);

        return Results.Created(
            $"/api/v1/payment-intents/{intent.PaymentIntentId}",
            intent);
    });

app.MapGet(
    "/api/v1/payment-intents/{paymentIntentId:guid}",
    async (
        Guid paymentIntentId,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        var intent =
            await service.GetAsync(
                paymentIntentId,
                cancellationToken);

        return intent is null
            ? Results.NotFound()
            : Results.Ok(intent);
    });

app.MapPost(
    "/api/v1/payment-intents/{paymentIntentId:guid}/authorize",
    async (
        Guid paymentIntentId,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.AuthorizeAsync(
                paymentIntentId,
                cancellationToken));
    });

app.MapPost(
    "/api/v1/payment-intents/{paymentIntentId:guid}/process",
    async (
        Guid paymentIntentId,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.StartProcessingAsync(
                paymentIntentId,
                cancellationToken));
    });

app.MapPost(
    "/api/v1/payment-intents/{paymentIntentId:guid}/complete",
    async (
        Guid paymentIntentId,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.CompleteAsync(
                paymentIntentId,
                cancellationToken));
    });

app.MapPost(
    "/api/v1/payment-intents/{paymentIntentId:guid}/cancel",
    async (
        Guid paymentIntentId,
        PaymentIntentService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.CancelAsync(
                paymentIntentId,
                cancellationToken));
    });

app.MapOpenApi();

app.Run();

public partial class Program;
