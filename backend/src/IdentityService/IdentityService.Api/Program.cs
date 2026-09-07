using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Endpoints;
using IdentityService.Api.Auth.Lifecycle;
using IdentityService.Api.Auth.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var jwtOptions = new JwtAccessTokenOptions(
    builder.Configuration["Auth:Jwt:Issuer"] ?? "https://identity.afrikawallet.local",
    builder.Configuration["Auth:Jwt:Audience"] ?? "afrikawallet-mobile",
    builder.Configuration["Auth:Jwt:SigningKey"] ??
        Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY") ??
        throw new InvalidOperationException("Auth JWT signing key is not configured."));

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<IRefreshTokenService, CryptographicRefreshTokenService>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton<JwtAccessTokenValidator>();
builder.Services.AddSingleton<IAuthSessionStore, InMemoryAuthSessionStore>();
builder.Services.AddSingleton<IAuthUserStore, InMemoryAuthUserStore>();
builder.Services.AddSingleton(AuthSessionOptions.Default);
builder.Services.AddSingleton(AuthApplicationOptions.Default);
builder.Services.AddSingleton<AuthSessionLifecycleService>();
builder.Services.AddSingleton<AuthApplicationService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();

app.Run();
