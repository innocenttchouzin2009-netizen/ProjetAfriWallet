using System.Security.Claims;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Handlers;
using IdentityService.Application.Services;
using IdentityService.Contracts.Requests;
using IdentityService.Contracts.Responses;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

builder.Services.AddSingleton<IPinHasher, Pbkdf2PinHasher>();
builder.Services.AddSingleton(new PinPolicy());
builder.Services.AddSingleton<IPinCredentialRepository, InMemoryPinCredentialRepository>();
builder.Services.AddSingleton<ITrustedDeviceRepository, InMemoryTrustedDeviceRepository>();
builder.Services.AddSingleton<CreatePinHandler>();
builder.Services.AddSingleton<VerifyPinHandler>();
builder.Services.AddSingleton<RegisterDeviceHandler>();
builder.Services.AddSingleton<RevokeDeviceHandler>();
builder.Services.AddSingleton<LoginHandler>();
builder.Services.AddSingleton<RefreshSessionHandler>();
builder.Services.AddSingleton<LogoutHandler>();
builder.Services.AddSingleton<LogoutAllHandler>();
builder.Services.AddSingleton<ITokenService>(new JwtTokenService("super-secret-key-123456"));
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<IAuthenticationEventRepository, InMemoryAuthenticationEventRepository>();
builder.Services.AddSingleton<IAuthenticationTimelineRepository, InMemoryAuthenticationTimelineRepository>();
builder.Services.AddSingleton<IAwidRepository, InMemoryAwidRepository>();
builder.Services.AddSingleton(new AwidPolicy());
builder.Services.AddSingleton<CreateAwidHandler>();
builder.Services.AddSingleton<ChangeAliasHandler>();
builder.Services.AddSingleton<CheckAliasAvailabilityHandler>();
builder.Services.AddSingleton<GetAwidProfileHandler>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/auth/register", () => Results.Ok(new { userId = Guid.NewGuid().ToString(), status = "PENDING" }));

app.MapPost("/api/v1/auth/pin", async (CreatePinRequest request, CreatePinHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/auth/pin/verify", async (VerifyPinRequest request, VerifyPinHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/devices", async (RegisterDeviceRequest request, RegisterDeviceHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapGet("/api/v1/devices", async (ITrustedDeviceRepository repository, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var devices = await repository.ListByUserIdAsync(userId, cancellationToken);
    return Results.Ok(devices.Select(x => new { x.DeviceId, x.DeviceName, x.Status, x.PublicKey }));
});

app.MapPost("/api/v1/devices/{deviceId}/revoke", async (string deviceId, RevokeDeviceHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(deviceId, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/auth/login", async (LoginRequest request, LoginHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/auth/refresh", async (RefreshRequest request, RefreshSessionHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/auth/logout", async (LogoutRequest request, LogoutHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/auth/logout-all", async (LogoutAllHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapGet("/api/v1/sessions", async (ISessionRepository repository, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var sessions = await repository.ListByUserIdAsync(userId, cancellationToken);
    return Results.Ok(sessions.Select(x => new { x.Id, x.DeviceId, x.Status, x.LastActivityAt, x.ExpiresAt }));
});

app.MapDelete("/api/v1/sessions/{sessionId}", async (Guid sessionId, LogoutHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(new LogoutRequest { SessionId = sessionId }, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPost("/api/v1/awids", async (CreateAwidRequest request, CreateAwidHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapPatch("/api/v1/awids/me/alias", async (ChangeAliasRequest request, ChangeAliasHandler handler, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(request, user, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapGet("/api/v1/awids/me", async (IAwidRepository repository, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var awid = await repository.GetBySubjectIdAsync(userId, cancellationToken);
    return awid is null ? Results.NotFound() : Results.Ok(new { awid.PublicAwid, alias = awid.AliasDisplay, status = awid.Status.ToString().ToUpperInvariant() });
});

app.MapGet("/api/v1/awids/aliases/{alias}/availability", async (string alias, CheckAliasAvailabilityHandler handler, CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(new GetAliasAvailabilityRequest { Alias = alias }, cancellationToken);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

app.MapGet("/api/v1/awids/me/alias/history", async (IAwidRepository repository, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var history = await repository.ListAliasHistoryAsync(userId, cancellationToken);
    return Results.Ok(history.Select(x => new
    {
        previousAlias = $"@{x.PreviousAlias}",
        newAlias = $"@{x.NewAlias}",
        x.ChangedAt,
        x.ReservedUntil
    }));
});

app.MapGet("/api/v1/awids/{publicAwid}", async (string publicAwid, IAwidRepository repository, CancellationToken cancellationToken) =>
{
    var awid = await repository.GetByPublicAwidAsync(publicAwid, cancellationToken);
    if (awid is null)
    {
        return Results.NotFound(new AwidPublicProfileResponse { Success = false, ErrorCode = "AWID_NOT_FOUND", Message = "AWID not found" });
    }

    if (awid.Status is AwidStatus.Suspended or AwidStatus.Closed)
    {
        return Results.NotFound(new AwidPublicProfileResponse { Success = false, ErrorCode = "AWID_NOT_FOUND", Message = "AWID not found" });
    }

    return awid.PrivacyMode switch
    {
        AwidPrivacyMode.Private => Results.Ok(new AwidPublicProfileResponse
        {
            Success = true,
            PublicAwid = awid.PublicAwid,
            Alias = awid.AliasDisplay,
            Status = awid.Status.ToString().ToUpperInvariant(),
            PrivacyMode = awid.PrivacyMode.ToString().ToUpperInvariant()
        }),
        _ => Results.Ok(new AwidPublicProfileResponse
        {
            Success = true,
            PublicAwid = awid.PublicAwid,
            Alias = awid.AliasDisplay,
            Status = awid.Status.ToString().ToUpperInvariant(),
            PrivacyMode = awid.PrivacyMode.ToString().ToUpperInvariant()
        })
    };
});

app.Run();

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "demo-user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
