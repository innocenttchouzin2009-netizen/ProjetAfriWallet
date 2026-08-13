using AfriWallet.BankingPlatform.BankProviderIntegration.Application;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Services;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Registries;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Repositories;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Security;
using AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBankProviderRegistry, InMemoryBankProviderRegistry>();
builder.Services.AddSingleton<IProviderTransferRepository, InMemoryProviderTransferRepository>();
builder.Services.AddSingleton<IRequestSigner, HmacRequestSigner>();
builder.Services.AddSingleton<IWebhookVerifier, HmacWebhookVerifier>();
builder.Services.AddSingleton<IProviderTelemetry, NoOpProviderTelemetry>();
builder.Services.AddScoped<BankProviderIntegrationService>();
builder.Services.AddScoped<BankWebhookService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { service = "afriwallet-bank-provider-integration", status = "healthy" }));

app.MapPost("/api/v1/banking/provider-transfers/submit", async (
    SubmitProviderTransferRequest request,
    BankProviderIntegrationService service,
    CancellationToken cancellationToken) =>
{
    var transfer = await service.SubmitAsync(request, cancellationToken);
    return Results.Ok(transfer);
});

app.MapPost("/api/v1/banking/provider-transfers/webhooks", (
    ProviderWebhookRequest request,
    BankWebhookService service,
    IConfiguration configuration) =>
{
    var secret = configuration["Sandbox:ProviderWebhookSecret"] ?? "sandbox-provider-secret";
    var result = service.Process(request, secret);
    return Results.Ok(result);
});

app.MapOpenApi();
app.Run();

public partial class Program;
