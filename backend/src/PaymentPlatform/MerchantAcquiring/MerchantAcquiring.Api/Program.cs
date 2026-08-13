using MerchantAcquiring.Application.Interfaces;
using MerchantAcquiring.Application.Services;
using MerchantAcquiring.Contracts.Requests;
using MerchantAcquiring.Infrastructure.MerchantRegistry;
using MerchantAcquiring.Infrastructure.PaymentRouting;
using MerchantAcquiring.Infrastructure.Repositories;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    IMerchantAcquiringRepository,
    InMemoryMerchantAcquiringRepository>();

builder.Services.AddSingleton<
    IMerchantRegistryGateway,
    SandboxMerchantRegistryGateway>();

builder.Services.AddSingleton<
    IPaymentRoutingGateway,
    SandboxPaymentRoutingGateway>();

builder.Services.AddSingleton<
    IAcquiringProcessorGateway,
    SandboxAcquiringProcessorGateway>();

builder.Services.AddSingleton<
    AcquiringFeeCalculator>();

builder.Services.AddScoped<
    MerchantAcquiringService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet(
    "/health/live",
    () => Results.Ok(new
    {
        status = "Healthy",
        service =
            "afriwallet-merchant-acquiring"
    }));

app.MapPost(
    "/api/v1/acquiring/merchants",
    async (
        CreateAcquiringProfileRequest request,
        MerchantAcquiringService service,
        CancellationToken cancellationToken) =>
    {
        var profile =
            await service.CreateProfileAsync(
                request.MerchantId,
                request.CountryCode,
                request.SettlementCurrency,
                cancellationToken);

        return Results.Created(
            $"/api/v1/acquiring/merchants/{profile.MerchantId}",
            profile);
    });

app.MapPost(
    "/api/v1/acquiring/payments",
    async (
        CreateMerchantPaymentRequest request,
        MerchantAcquiringService service,
        CancellationToken cancellationToken) =>
    {
        var payment =
            await service.CreatePaymentAsync(
                request.PaymentIntentId,
                request.MerchantId,
                request.CurrencyCode,
                request.AmountMinor,
                request.PaymentMethod,
                request.IdempotencyKey,
                cancellationToken);

        return Results.Created(
            $"/api/v1/acquiring/payments/{payment.PaymentId}",
            payment);
    });

app.MapPost(
    "/api/v1/acquiring/payments/{paymentId:guid}/authorize",
    async (
        Guid paymentId,
        AuthorizeMerchantPaymentRequest request,
        MerchantAcquiringService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.AuthorizeAsync(
                paymentId,
                request.CountryCode,
                cancellationToken));
    });

app.MapPost(
    "/api/v1/acquiring/payments/{paymentId:guid}/capture",
    async (
        Guid paymentId,
        MerchantAcquiringService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.CaptureAsync(
                paymentId,
                cancellationToken));
    });

app.MapPost(
    "/api/v1/acquiring/payments/{paymentId:guid}/refunds",
    async (
        Guid paymentId,
        RefundMerchantPaymentRequest request,
        MerchantAcquiringService service,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await service.RefundAsync(
                paymentId,
                request.AmountMinor,
                request.Reason,
                cancellationToken));
    });

app.MapOpenApi();

app.Run();

public partial class Program;
