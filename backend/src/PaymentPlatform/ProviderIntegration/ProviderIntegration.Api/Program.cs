using AfriWallet.PaymentPlatform.ProviderIntegration.Application;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Credentials;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Health;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Providers;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Secrets;
using AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Webhooks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProviderSecretSource, EnvironmentSecretSource>();
builder.Services.AddSingleton<IProviderCredentialService, SandboxCredentialService>();
builder.Services.AddSingleton<IProviderHealthService, InMemoryProviderHealthService>();
builder.Services.AddSingleton<IProviderExecutor, SandboxProviderExecutor>();
builder.Services.AddSingleton<IProviderWebhookVerifier, HmacWebhookVerifier>();
builder.Services.AddSingleton<RetryPolicy>();
builder.Services.AddSingleton<ProviderIntegrationService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "payment-provider-integration",
    status = "healthy",
    providerMode = "sandbox"
}));

app.MapPost(
    "/api/v1/provider-integration/execute",
    async (
        ProviderExecutionRequest request,
        ProviderIntegrationService service,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.ExecuteAsync(
                request,
                maxRetries: 2,
                cancellationToken);

            if (result.Success)
                return Results.Ok(result);

            return Results.Json(
                result,
                statusCode: result.Retryable
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status422UnprocessableEntity);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_request",
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/api/v1/provider-integration/providers/{providerCode}/health",
    (string providerCode, IProviderHealthService health) =>
    {
        try
        {
            return Results.Ok(health.Get(providerCode));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_provider",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/api/v1/provider-integration/webhooks/{providerCode}/verify",
    (
        string providerCode,
        ProviderWebhookVerificationRequest request,
        IProviderWebhookVerifier verifier) =>
    {
        if (!providerCode.Equals(
                request.ProviderCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                error = "provider_mismatch"
            });
        }

        try
        {
            return verifier.Verify(request)
                ? Results.Ok(new { verified = true })
                : Results.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(
                title: "Webhook verification is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_webhook",
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/api/v1/provider-integration/operations/audit",
    (ProviderIntegrationService service) => Results.Ok(service.AuditEvents));

app.MapGet(
    "/api/v1/provider-integration/operations/telemetry",
    (ProviderIntegrationService service) => Results.Ok(service.TelemetryEvents));

app.Run();

public partial class Program;