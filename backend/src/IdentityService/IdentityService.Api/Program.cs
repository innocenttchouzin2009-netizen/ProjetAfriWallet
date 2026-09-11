using System.Text;
using System.Text.Json;
using AfriWallet.Balance.Application;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Persistence;
using AfriWallet.P2P.Directory.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Persistence;
using IdentityService.Api.Auth.Abstractions;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Endpoints;
using IdentityService.Api.Auth.Lifecycle;
using IdentityService.Api.Auth.Persistence;
using IdentityService.Api.Auth.Security;
using IdentityService.Api.Balance;
using IdentityService.Api.Fx;
using IdentityService.Api.Ledger;
using IdentityService.Api.P2P;
using IdentityService.Api.Transfer;
using IdentityService.Api.Wallet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var jwtOptions = new JwtAccessTokenOptions(
    builder.Configuration["Auth:Jwt:Issuer"] ?? "https://identity.afrikawallet.local",
    builder.Configuration["Auth:Jwt:Audience"] ?? "afrikawallet-mobile",
    builder.Configuration["Auth:Jwt:SigningKey"] ??
        Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY") ??
        throw new InvalidOperationException("Auth JWT signing key is not configured."));

var authConnectionString = builder.Configuration.GetConnectionString("AuthDatabase") ??
    throw new InvalidOperationException("Auth database connection string is not configured.");

var walletConnectionString = builder.Configuration.GetConnectionString("WalletDatabase") ??
    Environment.GetEnvironmentVariable("AFW_WALLET_DB_CONNECTION_STRING") ??
    "Data Source=wallet-registry.db";

var ledgerConnectionString = builder.Configuration.GetConnectionString("LedgerDatabase") ??
    Environment.GetEnvironmentVariable("AFW_LEDGER_DB_CONNECTION_STRING") ??
    "Data Source=universal-ledger.db";

var recipientDirectoryConnectionString = builder.Configuration.GetConnectionString("P2PRecipientDirectory") ??
    Environment.GetEnvironmentVariable("AFW_P2P_RECIPIENT_DIRECTORY_DB_CONNECTION_STRING") ??
    "Data Source=p2p-recipient-directory.db";

var configuredFxRates = FxConfiguration.LoadRates(builder.Configuration);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite(authConnectionString));
builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseSqlite(walletConnectionString));
builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseSqlite(ledgerConnectionString));

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<IRefreshTokenService, CryptographicRefreshTokenService>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton<JwtAccessTokenValidator>();
builder.Services.AddScoped<IAuthSessionStore, EfAuthSessionStore>();
builder.Services.AddScoped<IAuthUserStore, EfAuthUserStore>();
builder.Services.AddSingleton(AuthSessionOptions.Default);
builder.Services.AddSingleton(AuthApplicationOptions.Default);
builder.Services.AddScoped<AuthSessionLifecycleService>();
builder.Services.AddScoped<AuthApplicationService>();

builder.Services.AddScoped<IWalletRepository, EfWalletRepository>();
builder.Services.AddSingleton<ISupportedCurrencyPolicy, ConfiguredSupportedCurrencyPolicy>();
builder.Services.AddScoped<WalletRegistryApplicationService>();

builder.Services.AddScoped<IJournalRepository, EfJournalRepository>();
builder.Services.AddScoped<LedgerPostingApplicationService>();

builder.Services.AddScoped<ILedgerJournalReader, EfLedgerJournalReader>();
builder.Services.AddScoped<BalanceProjectionService>();
builder.Services.AddScoped<LedgerBackedBalanceReadService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IFxQuoteProvider>(services =>
    new ConfiguredFxQuoteProvider(
        configuredFxRates,
        services.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<FxQuoteApplicationService>();

builder.Services.AddInternalTransferModule(builder.Configuration);
builder.Services.AddP2PCore();
builder.Services.AddAuthoritativeP2PRecipientDirectory(recipientDirectoryConnectionString);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal is null ||
                    !Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId) ||
                    !Guid.TryParse(principal.FindFirst("sid")?.Value, out var sessionId) ||
                    !long.TryParse(principal.FindFirst("ver")?.Value, out var tokenVersion))
                {
                    context.HttpContext.Items["AuthErrorCode"] = AuthErrorCode.TokenInvalid;
                    context.HttpContext.Items["AuthErrorMessage"] = "Invalid access token claims.";
                    context.Fail("Invalid access token claims.");
                    return;
                }

                var auth = context.HttpContext.RequestServices.GetRequiredService<AuthApplicationService>();
                var session = await auth.GetSessionAsync(userId, sessionId, context.HttpContext.RequestAborted);
                if (!session.Succeeded || session.Value is null)
                {
                    context.HttpContext.Items["AuthErrorCode"] = session.ErrorCode ?? AuthErrorCode.TokenInvalid;
                    context.HttpContext.Items["AuthErrorMessage"] = session.ErrorMessage ?? "Invalid session.";
                    context.Fail(session.ErrorMessage ?? "Invalid session.");
                    return;
                }

                if (session.Value.TokenVersion != tokenVersion)
                {
                    context.HttpContext.Items["AuthErrorCode"] = AuthErrorCode.TokenInvalid;
                    context.HttpContext.Items["AuthErrorMessage"] = "Stale access token.";
                    context.Fail("Stale access token.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var code = context.HttpContext.Items["AuthErrorCode"] as string ?? AuthErrorCode.TokenInvalid;
                var message = context.HttpContext.Items["AuthErrorMessage"] as string ?? "Invalid or missing access token.";
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    new AuthErrorResponse(code, message, context.HttpContext.TraceIdentifier),
                    cancellationToken: context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    new AuthErrorResponse(
                        AuthErrorCode.UserDisabled,
                        "Access is forbidden.",
                        context.HttpContext.TraceIdentifier),
                    cancellationToken: context.HttpContext.RequestAborted);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapWalletEndpoints();
app.MapLedgerEndpoints();
app.MapBalanceEndpoints();
app.MapFxEndpoints();
app.MapTransferEndpoints();
app.MapP2PEndpoints();

app.Run();
