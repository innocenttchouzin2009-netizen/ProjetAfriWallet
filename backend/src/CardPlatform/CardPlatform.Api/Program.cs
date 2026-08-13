using AfriWallet.CardPlatform.Api.Production;
using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Services.Configure<CardProductionConfiguration>(builder.Configuration.GetSection(CardProductionConfiguration.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<CardProductionConfiguration>>().Value);
builder.Services.AddSingleton<CardProductionConfigurationService>();
builder.Services.AddSingleton<CardFeatureFlags>();
builder.Services.AddSingleton<CardHealthProbe>();
builder.Services.AddSingleton<CardAuditService>();
builder.Services.AddSingleton<CardTelemetry>();
builder.Services.AddCardResilience();
builder.Services.AddCardRateLimiting();
builder.Services.AddSingleton<ICardProgramRepository, InMemoryCardProgramRepository>();
builder.Services.AddSingleton<CardProgramService>();
builder.Services.AddSingleton<IVirtualCardRepository, InMemoryVirtualCardRepository>();
builder.Services.AddSingleton<VirtualCardService>();
builder.Services.AddSingleton<ICardAuthorizationRepository, InMemoryAuthorizationRepository>();
builder.Services.AddSingleton<CardAuthorizationService>();
builder.Services.AddSingleton<ITokenRepository, InMemoryTokenRepository>();
builder.Services.AddSingleton<TokenizationService>();
builder.Services.AddSingleton<TokenVault>();
builder.Services.AddSingleton<TokenValidator>();
builder.Services.AddSingleton<IWalletProvisioningRepository, InMemoryWalletProvisioningRepository>();
builder.Services.AddSingleton<WalletProvisioningService>();
builder.Services.AddSingleton<ICardLifecycleRepository, InMemoryCardRepository>();
builder.Services.AddSingleton<CardLifecycleService>();

var app = builder.Build();

app.UseMiddleware<CardCorrelationMiddleware>();
app.UseRateLimiter();

app.MapGet("/health/live", (CardHealthProbe probe) =>
{
    var checks = probe.Check();
    var live = checks.Values.All(v => v);
    return Results.Ok(new { status = live ? "live" : "degraded", checks });
});
app.MapGet("/health/ready", (CardHealthProbe probe) =>
{
    var checks = probe.Check();
    var ready = checks.Values.All(v => v);
    return Results.Ok(new { status = ready ? "ready" : "degraded", checks });
});
app.MapGet("/health/startup", () => Results.Ok(new { status = "startup" }));
app.MapGet("/api/v1/production/configuration", (CardProductionConfigurationService service) => Results.Ok(service.GetSummary()));
app.MapGet("/api/v1/production/feature-flags", (CardFeatureFlags flags) => Results.Ok(flags));
app.MapPost("/api/v1/production/audit", (CardAuditService audit, string action, string subjectId, string correlationId, string? cardId = null, string? tokenId = null, string? authorizationId = null, string? workflowId = null) =>
{
    audit.Record(action, subjectId, correlationId, cardId, tokenId, authorizationId, workflowId);
    return Results.Ok(new { status = "recorded" });
});
app.MapGet("/api/v1/production/metrics", (CardTelemetry telemetry) => Results.Ok(new { status = "ok" }));

app.MapGet("/api/v1/card-programs", async (CardProgramService service, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    logger.LogCardEvent("card-registry-query", correlationId: httpContext.Items["CardCorrelationContext"] is CardCorrelationContext correlation ? correlation.CorrelationId : null, traceId: httpContext.Items["CardCorrelationContext"] is CardCorrelationContext trace ? trace.TraceId : null);
    var programs = await service.GetAllAsync(cancellationToken);
    return Results.Ok(programs);
}).RequireRateLimiting("card-registry");

app.MapGet("/api/v1/card-programs/{programId}", async (string programId, CardProgramService service, CancellationToken cancellationToken) =>
{
    var program = await service.GetByIdAsync(programId, cancellationToken);
    return program is null ? Results.NotFound() : Results.Ok(program);
});

app.MapPost("/internal/card-programs", async (CardProgram request, CardProgramService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/card-programs/{created.ProgramId}", created);
});

app.MapPut("/internal/card-programs/{programId}", async (string programId, CardProgram request, CardProgramService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateAsync(programId, request, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapPost("/api/v1/cards/virtual", async (VirtualCard request, VirtualCardService service, CardTelemetry telemetry, CardAuditService audit, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    telemetry.TrackCardIssued();
    var correlation = httpContext.Items["CardCorrelationContext"] as CardCorrelationContext;
    audit.Record("card-issued", request.VirtualCardId, correlation?.CorrelationId ?? "n/a", request.VirtualCardId);
    logger.LogCardEvent("card-issued", correlationId: correlation?.CorrelationId, traceId: correlation?.TraceId, cardId: request.VirtualCardId);
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/cards/{created.VirtualCardId}", created);
}).RequireRateLimiting("virtual-cards");

app.MapGet("/api/v1/cards", async (VirtualCardService service, CancellationToken cancellationToken) =>
{
    var cards = await service.GetAllAsync(cancellationToken);
    return Results.Ok(cards);
});

app.MapGet("/api/v1/cards/{cardId}", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.GetByIdAsync(cardId, cancellationToken);
    return card is null ? Results.NotFound() : Results.Ok(card);
});

app.MapPut("/api/v1/cards/{cardId}/controls", async (string cardId, bool ecommerceEnabled, bool contactlessEnabled, bool internationalEnabled, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.UpdateControlsAsync(cardId, ecommerceEnabled, contactlessEnabled, internationalEnabled, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid controls update" }) : Results.Ok(card);
});

app.MapPut("/api/v1/cards/{cardId}/limits", async (string cardId, long spendingLimitMinor, long dailyLimitMinor, long monthlyLimitMinor, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.UpdateLimitsAsync(cardId, spendingLimitMinor, dailyLimitMinor, monthlyLimitMinor, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid limits update" }) : Results.Ok(card);
});

app.MapGet("/api/v1/cards/{cardId}/audit", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var trail = await service.GetAuditTrailAsync(cardId, cancellationToken);
    return Results.Ok(new { cardId, trail });
});

app.MapPost("/api/v1/cards/authorize", async (CardAuthorizationRequest request, CardAuthorizationService service, CardTelemetry telemetry, CardAuditService audit, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var correlation = httpContext.Items["CardCorrelationContext"] as CardCorrelationContext;
    var result = await service.AuthorizeAsync(request, cancellationToken);
    telemetry.TrackAuthorization(result.Decision == "AUTHORIZED");
    audit.Record(result.Decision == "AUTHORIZED" ? "authorization-approved" : "authorization-declined", request.CardId, correlation?.CorrelationId ?? "n/a", request.CardId, authorizationId: result.AuthorizationId);
    logger.LogCardEvent("authorization-processed", correlationId: correlation?.CorrelationId, traceId: correlation?.TraceId, cardId: request.CardId, authorizationId: result.AuthorizationId);
    return Results.Ok(result);
}).RequireRateLimiting("authorizations");

app.MapPost("/api/v1/cards/preauthorize", async (CardAuthorizationRequest request, CardAuthorizationService service, CancellationToken cancellationToken) =>
{
    var result = await service.AuthorizeAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapPost("/api/v1/cards/reverse", async (CardAuthorizationReverseRequest request, CardAuthorizationService service, CancellationToken cancellationToken) =>
{
    var result = await service.ReverseAsync(request, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/v1/cards/authorizations/{authorizationId}", async (string authorizationId, CardAuthorizationService service, CancellationToken cancellationToken) =>
{
    var authorization = await service.GetByIdAsync(authorizationId, cancellationToken);
    return authorization is null ? Results.NotFound() : Results.Ok(authorization);
});

app.MapPost("/api/v1/cards/{cardId}/tokens", async (string cardId, CardTokenRequest request, TokenizationService service, CardTelemetry telemetry, CardAuditService audit, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var correlation = httpContext.Items["CardCorrelationContext"] as CardCorrelationContext;
    var token = await service.CreateAsync(new CardTokenRequest
    {
        CardId = cardId,
        OwnerAwidId = request.OwnerAwidId,
        WalletId = request.WalletId,
        Network = request.Network,
        TokenType = request.TokenType
    }, cancellationToken);
    telemetry.TrackTokenIssued();
    audit.Record("token-created", cardId, correlation?.CorrelationId ?? "n/a", cardId, tokenId: token?.TokenId);
    logger.LogCardEvent("token-created", correlationId: correlation?.CorrelationId, traceId: correlation?.TraceId, cardId: cardId, tokenId: token?.TokenId);
    return token is null ? Results.BadRequest() : Results.Created($"/api/v1/tokens/{token.TokenId}", token);
}).RequireRateLimiting("tokenization");

app.MapGet("/api/v1/cards/{cardId}/tokens", async (string cardId, TokenizationService service, CancellationToken cancellationToken) =>
{
    var tokens = await service.GetTokensForCardAsync(cardId, cancellationToken);
    return Results.Ok(tokens);
});

app.MapPost("/api/v1/tokens/{tokenId}/suspend", async (string tokenId, TokenizationService service, CancellationToken cancellationToken) =>
{
    var token = await service.SuspendAsync(tokenId, cancellationToken);
    return token is null ? Results.BadRequest() : Results.Ok(token);
});

app.MapPost("/api/v1/tokens/{tokenId}/resume", async (string tokenId, TokenizationService service, CancellationToken cancellationToken) =>
{
    var token = await service.ResumeAsync(tokenId, cancellationToken);
    return token is null ? Results.BadRequest() : Results.Ok(token);
});

app.MapPost("/api/v1/tokens/{tokenId}/revoke", async (string tokenId, TokenizationService service, CancellationToken cancellationToken) =>
{
    var token = await service.RevokeAsync(tokenId, cancellationToken);
    return token is null ? Results.BadRequest() : Results.Ok(token);
});

app.MapPost("/api/v1/tokens/{tokenId}/rotate", async (string tokenId, TokenizationService service, CancellationToken cancellationToken) =>
{
    var token = await service.RotateAsync(tokenId, cancellationToken);
    return token is null ? Results.BadRequest() : Results.Ok(token);
});

app.MapPost("/api/v1/cards/{cardId}/wallet-provisioning/eligibility", async (string cardId, WalletProvisioningRequest request, WalletProvisioningService provisioningService, VirtualCardService cardService, CancellationToken cancellationToken) =>
{
    var card = await cardService.GetByIdAsync(cardId, cancellationToken);
    if (card is null) return Results.NotFound();

    var eligible = await provisioningService.ValidateEligibilityAsync(card, request.Provider, request.Environment, cancellationToken);
    return Results.Ok(new { cardId, eligible, provider = request.Provider, environment = request.Environment });
});

app.MapPost("/api/v1/cards/{cardId}/wallet-provisioning", async (string cardId, WalletProvisioningRequest request, WalletProvisioningService service, CardTelemetry telemetry, CardAuditService audit, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var correlation = httpContext.Items["CardCorrelationContext"] as CardCorrelationContext;
    var result = await service.ProvisionAsync(new WalletProvisioningRequest
    {
        CardId = cardId,
        Provider = request.Provider,
        WalletId = request.WalletId,
        Environment = request.Environment
    }, cancellationToken);
    telemetry.TrackWalletProvisioning();
    audit.Record(result?.Status == "PROVISIONED" ? "wallet-provisioned" : "wallet-provisioning-failed", cardId, correlation?.CorrelationId ?? "n/a", cardId, workflowId: result?.ProvisioningId);
    logger.LogCardEvent("wallet-provisioned", correlationId: correlation?.CorrelationId, traceId: correlation?.TraceId, cardId: cardId, workflowId: result?.ProvisioningId);
    return Results.Ok(result);
}).RequireRateLimiting("wallet-provisioning");

app.MapPost("/api/v1/wallet-provisioning/{provisioningId}/suspend", async (string provisioningId, WalletProvisioningService service, CancellationToken cancellationToken) =>
{
    var provisioning = await service.SuspendAsync(provisioningId, cancellationToken);
    return provisioning is null ? Results.BadRequest() : Results.Ok(provisioning);
});

app.MapPost("/api/v1/wallet-provisioning/{provisioningId}/resume", async (string provisioningId, WalletProvisioningService service, CancellationToken cancellationToken) =>
{
    var provisioning = await service.ResumeAsync(provisioningId, cancellationToken);
    return provisioning is null ? Results.BadRequest() : Results.Ok(provisioning);
});

app.MapPost("/api/v1/wallet-provisioning/{provisioningId}/remove", async (string provisioningId, WalletProvisioningService service, CancellationToken cancellationToken) =>
{
    var provisioning = await service.RemoveAsync(provisioningId, cancellationToken);
    return provisioning is null ? Results.BadRequest() : Results.Ok(provisioning);
});

app.MapGet("/api/v1/cards/{cardId}/wallet-provisioning/audit", async (string cardId, WalletProvisioningService service, CancellationToken cancellationToken) =>
{
    var trail = await service.GetAuditTrailAsync(cardId, cancellationToken);
    return Results.Ok(new { cardId, trail });
});

app.MapPost("/api/v1/cards/{cardId}/activate", async (string cardId, CardLifecycleService service, CardTelemetry telemetry, CardAuditService audit, ILogger<Program> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var correlation = httpContext.Items["CardCorrelationContext"] as CardCorrelationContext;
    var card = await service.ActivateAsync(cardId, cancellationToken);
    telemetry.TrackLifecycleEvent();
    audit.Record("card-activated", cardId, correlation?.CorrelationId ?? "n/a", cardId);
    logger.LogCardEvent("card-activated", correlationId: correlation?.CorrelationId, traceId: correlation?.TraceId, cardId: cardId);
    return card is null ? Results.BadRequest() : Results.Ok(card);
}).RequireRateLimiting("lifecycle");

app.MapPost("/api/v1/cards/{cardId}/freeze", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.FreezeAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/unfreeze", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.UnfreezeAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/suspend", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.SuspendAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/resume", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.ResumeAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/replace", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.ReplaceAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/expire", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.ExpireAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/close", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.CloseAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest() : Results.Ok(card);
});

app.MapGet("/api/v1/cards/{cardId}/lifecycle", async (string cardId, CardLifecycleService service, CancellationToken cancellationToken) =>
{
    var card = await service.GetByIdAsync(cardId, cancellationToken);
    return card is null ? Results.NotFound() : Results.Ok(card);
});

app.Run();
