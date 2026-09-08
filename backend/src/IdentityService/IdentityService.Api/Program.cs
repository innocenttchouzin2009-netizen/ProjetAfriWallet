using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Endpoints;
using IdentityService.Api.Auth.Lifecycle;
using IdentityService.Api.Auth.Persistence;
using IdentityService.Api.Auth.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var jwtOptions = new JwtAccessTokenOptions(
    builder.Configuration["Auth:Jwt:Issuer"] ?? "https://identity.afrikawallet.local",
    builder.Configuration["Auth:Jwt:Audience"] ?? "afrikawallet-mobile",
    builder.Configuration["Auth:Jwt:SigningKey"] ??
        Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY") ??
        throw new InvalidOperationException("Auth JWT signing key is not configured."));

var authConnectionString =
    builder.Configuration.GetConnectionString("Auth") ??
    Environment.GetEnvironmentVariable("AFW_AUTH_DB_CONNECTION") ??
    throw new InvalidOperationException("Auth database connection string is not configured.");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<IRefreshTokenService, CryptographicRefreshTokenService>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton<JwtAccessTokenValidator>();
builder.Services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(authConnectionString));
builder.Services.AddScoped<IAuthSessionStore, EfAuthSessionStore>();
builder.Services.AddScoped<IAuthUserStore, EfAuthUserStore>();
builder.Services.AddSingleton(AuthSessionOptions.Default);
builder.Services.AddSingleton(AuthApplicationOptions.Default);
builder.Services.AddScoped<AuthSessionLifecycleService>();
builder.Services.AddScoped<AuthApplicationService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();

app.Run();
