using System.Security.Claims;
using System.Text.Encodings.Web;
using IdentityService.Api.Engine;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

builder.Services.AddSingleton<IIdentityRepository, InMemoryIdentityRepository>();
builder.Services.AddSingleton<IdentityCardService>();
builder.Services.AddSingleton<QrTokenService>();
builder.Services.AddSingleton<PrivacyResolver>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/v1/me/card", (
    ClaimsPrincipal user,
    IIdentityRepository repository,
    IdentityCardService cardService,
    IdentityCardContext? context) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    var account = repository.GetOrCreateAccount(subjectId);
    var card = cardService.BuildCard(account, context ?? IdentityCardContext.Personal);

    return Results.Ok(new MeCardResponse
    {
        Alias = card.Alias,
        PublicAwid = card.PublicAwid,
        DisplayName = card.DisplayName,
        PrivacyMode = card.PrivacyMode.ToString().ToUpperInvariant(),
        Theme = card.Theme,
        Context = card.Context.ToString().ToUpperInvariant(),
        VerificationBadges = card.VerificationBadges,
        BusinessName = card.BusinessName,
        AssociationName = card.AssociationName,
        BusinessHours = card.BusinessHours,
        UpdatedAt = card.UpdatedAt
    });
});

app.MapGet("/api/v1/me/qr", (
    ClaimsPrincipal user,
    IIdentityRepository repository,
    QrTokenService qrService) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    var permanent = repository.GetPermanentIdentityToken(subjectId);
    if (permanent is null)
    {
        var account = repository.GetOrCreateAccount(subjectId);
        permanent = qrService.CreateSignedToken(account, QrType.Identity, "IDENTITY_SHARE", null, int.MaxValue, null, null);
        repository.CreateQrToken(permanent);
        repository.AddAudit(new AuditEvent { EventType = "QR_CREATED", SubjectId = subjectId, QrId = permanent.Id, Details = "Permanent identity QR created" });
    }

    return Results.Ok(ToResponse(permanent));
});

app.MapPost("/api/v1/me/qr/temp", (
    ClaimsPrincipal user,
    CreateTempQrRequest request,
    IIdentityRepository repository,
    QrTokenService qrService) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    var account = repository.GetOrCreateAccount(subjectId);
    var expiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Clamp(request.ExpiresInMinutes, 1, 60 * 24));
    var token = qrService.CreateSignedToken(account, request.Type, request.Purpose, expiresAt, request.MaxUses, request.Amount, request.Currency);
    repository.CreateQrToken(token);
    repository.AddAudit(new AuditEvent { EventType = "QR_CREATED", SubjectId = subjectId, QrId = token.Id, Details = $"Temporary {request.Type} QR created" });

    return Results.Ok(ToResponse(token));
});

app.MapPost("/api/v1/me/qr/payment", (
    ClaimsPrincipal user,
    CreatePaymentQrRequest request,
    IIdentityRepository repository,
    QrTokenService qrService) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    if (request.Amount <= 0)
    {
        return Results.BadRequest(new { errorCode = "AMOUNT_INVALID", message = "Amount must be greater than zero" });
    }

    var account = repository.GetOrCreateAccount(subjectId);
    var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
    var token = qrService.CreateSignedToken(account, QrType.Payment, "RECEIVE_PAYMENT", expiresAt, 1, request.Amount, request.Currency);
    repository.CreateQrToken(token);
    repository.AddAudit(new AuditEvent { EventType = "QR_CREATED", SubjectId = subjectId, QrId = token.Id, Details = "Payment QR created" });

    return Results.Ok(ToResponse(token));
});

app.MapDelete("/api/v1/me/qr/{id:guid}", (
    ClaimsPrincipal user,
    Guid id,
    IIdentityRepository repository) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    repository.RevokeQrToken(id, subjectId);
    repository.AddAudit(new AuditEvent { EventType = "QR_REVOKED", SubjectId = subjectId, QrId = id, Details = "QR revoked by owner" });
    return Results.NoContent();
});

app.MapPost("/api/v1/qr/resolve", (
    ResolveQrRequest request,
    IIdentityRepository repository,
    QrTokenService qrService,
    PrivacyResolver privacyResolver) =>
{
    var result = qrService.Resolve(request.Token, request.ExpectedType, repository, privacyResolver);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/v1/me/card/share", (ClaimsPrincipal user, IIdentityRepository repository) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    repository.AddAudit(new AuditEvent { EventType = "CARD_SHARED", SubjectId = subjectId, Details = "Digital identity card shared" });
    return Results.Ok(new { success = true, status = "SHARED" });
});

app.MapPost("/api/v1/me/card/download", (ClaimsPrincipal user, IIdentityRepository repository) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    repository.AddAudit(new AuditEvent { EventType = "CARD_DOWNLOADED", SubjectId = subjectId, Details = "Digital identity card downloaded" });
    return Results.Ok(new { success = true, status = "DOWNLOADED" });
});

app.MapGet("/api/v1/me/audit", (ClaimsPrincipal user, IIdentityRepository repository) =>
{
    var subjectId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(subjectId))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(repository.ListAuditEvents(subjectId));
});

app.MapGet("/auth/register", () => Results.Ok(new { userId = Guid.NewGuid().ToString(), status = "PENDING" }));

app.Run();

static QrTokenResponse ToResponse(QrToken token)
{
    return new QrTokenResponse
    {
        Id = token.Id,
        Token = token.Token,
        Type = token.Type.ToString().ToUpperInvariant(),
        Purpose = token.Purpose,
        ExpiresAt = token.ExpiresAt,
        MaxUses = token.MaxUses,
        UseCount = token.UseCount
    };
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
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
