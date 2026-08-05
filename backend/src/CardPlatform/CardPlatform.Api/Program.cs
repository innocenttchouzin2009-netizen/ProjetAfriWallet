using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapGet("/api/v1/card-programs", async (CardProgramService service, CancellationToken cancellationToken) =>
{
    var programs = await service.GetAllAsync(cancellationToken);
    return Results.Ok(programs);
});

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

app.MapPost("/api/v1/cards/virtual", async (VirtualCard request, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/cards/{created.VirtualCardId}", created);
});

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

app.MapPost("/api/v1/cards/{cardId}/activate", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.ActivateAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid transition" }) : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/freeze", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.FreezeAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid transition" }) : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/unfreeze", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.UnfreezeAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid transition" }) : Results.Ok(card);
});

app.MapPost("/api/v1/cards/{cardId}/close", async (string cardId, VirtualCardService service, CancellationToken cancellationToken) =>
{
    var card = await service.CloseAsync(cardId, cancellationToken);
    return card is null ? Results.BadRequest(new { message = "invalid transition" }) : Results.Ok(card);
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

app.MapPost("/api/v1/cards/authorize", async (CardAuthorizationRequest request, CardAuthorizationService service, CancellationToken cancellationToken) =>
{
    var result = await service.AuthorizeAsync(request, cancellationToken);
    return Results.Ok(result);
});

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

app.MapPost("/api/v1/cards/{cardId}/tokens", async (string cardId, CardTokenRequest request, TokenizationService service, CancellationToken cancellationToken) =>
{
    var token = await service.CreateAsync(new CardTokenRequest
    {
        CardId = cardId,
        OwnerAwidId = request.OwnerAwidId,
        WalletId = request.WalletId,
        Network = request.Network,
        TokenType = request.TokenType
    }, cancellationToken);
    return token is null ? Results.BadRequest() : Results.Created($"/api/v1/tokens/{token.TokenId}", token);
});

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

app.Run();
