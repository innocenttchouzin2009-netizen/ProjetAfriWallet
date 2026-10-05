using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MobileMoney.Production.Authentication;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Extensions;

var tests = new (string Name, Func<Task> Run)[]
{
    ("configured issuer, audience and signing key are reused", RunSync(ConfiguredValuesAreReused)),
    ("JWT validation is strict and claim mapping stays disabled", RunSync(ValidationIsStrict)),
    ("AFW_AUTH_JWT_SIGNING_KEY is supported as fallback", RunSync(EnvironmentSigningKeyIsSupported)),
    ("missing signing key fails closed", RunSync(MissingSigningKeyFailsClosed)),
    ("payout route rejects unauthenticated requests with 401", PayoutRouteRejectsUnauthenticatedAsync),
    ("payout route accepts authenticated subject", PayoutRouteAcceptsAuthenticatedSubjectAsync),
    ("payout route rejects malformed sub with 401", PayoutRouteRejectsMalformedSubjectAsync)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.Error.WriteLine($"FAIL: {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("MobileMoney JWT resource server scenarios passed.");

static Func<Task> RunSync(Action action) => () =>
{
    action();
    return Task.CompletedTask;
};

static void ConfiguredValuesAreReused()
{
    const string issuer = "https://identity.afrikawallet.test";
    const string audience = "afrikawallet-mobile-test";
    const string signingKey = "mobile-money-jwt-signing-key-for-tests-1234567890";

    var options = BuildOptions(new Dictionary<string, string?>
    {
        ["Auth:Jwt:Issuer"] = issuer,
        ["Auth:Jwt:Audience"] = audience,
        ["Auth:Jwt:SigningKey"] = signingKey
    });

    AssertEqual(issuer, options.TokenValidationParameters.ValidIssuer, "issuer");
    AssertEqual(audience, options.TokenValidationParameters.ValidAudience, "audience");

    var symmetricKey = options.TokenValidationParameters.IssuerSigningKey as SymmetricSecurityKey
        ?? throw new InvalidOperationException("issuer signing key must be symmetric");
    AssertEqual(signingKey, Encoding.UTF8.GetString(symmetricKey.Key), "signing key");
}

static void ValidationIsStrict()
{
    var options = BuildOptions(new Dictionary<string, string?>
    {
        ["Auth:Jwt:SigningKey"] = "strict-validation-signing-key-for-tests-1234567890"
    });

    AssertFalse(options.MapInboundClaims, "MapInboundClaims");
    AssertTrue(options.TokenValidationParameters.ValidateIssuerSigningKey, "ValidateIssuerSigningKey");
    AssertTrue(options.TokenValidationParameters.ValidateIssuer, "ValidateIssuer");
    AssertTrue(options.TokenValidationParameters.ValidateAudience, "ValidateAudience");
    AssertTrue(options.TokenValidationParameters.ValidateLifetime, "ValidateLifetime");
    AssertEqual(TimeSpan.Zero, options.TokenValidationParameters.ClockSkew, "ClockSkew");
    AssertEqual("https://identity.afrikawallet.local", options.TokenValidationParameters.ValidIssuer, "default issuer");
    AssertEqual("afrikawallet-mobile", options.TokenValidationParameters.ValidAudience, "default audience");
}

static void EnvironmentSigningKeyIsSupported()
{
    const string environmentKey = "mobile-money-environment-signing-key-for-tests-1234567890";
    var previous = Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY");

    try
    {
        Environment.SetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY", environmentKey);
        var options = BuildOptions(new Dictionary<string, string?>());

        var symmetricKey = options.TokenValidationParameters.IssuerSigningKey as SymmetricSecurityKey
            ?? throw new InvalidOperationException("issuer signing key must be symmetric");
        AssertEqual(environmentKey, Encoding.UTF8.GetString(symmetricKey.Key), "environment signing key");
    }
    finally
    {
        Environment.SetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY", previous);
    }
}

static void MissingSigningKeyFailsClosed()
{
    var previous = Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY");

    try
    {
        Environment.SetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY", null);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();

        try
        {
            services.AddMobileMoneyJwtAuthentication(configuration);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("signing key", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException("missing signing key did not fail closed");
    }
    finally
    {
        Environment.SetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY", previous);
    }
}

static async Task PayoutRouteRejectsUnauthenticatedAsync()
{
    await using var app = await BuildPayoutTestAppAsync();
    using var client = app.GetTestClient();

    var response = await client.PostAsJsonAsync(
        "/api/v1/mobile-money/payouts/eligibility",
        ValidEligibilityRequest());

    AssertEqual(HttpStatusCode.Unauthorized, response.StatusCode, "unauthenticated payout status");
}

static async Task PayoutRouteAcceptsAuthenticatedSubjectAsync()
{
    await using var app = await BuildPayoutTestAppAsync();
    using var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, Guid.NewGuid().ToString());

    var response = await client.PostAsJsonAsync(
        "/api/v1/mobile-money/payouts/eligibility",
        ValidEligibilityRequest());

    AssertEqual(HttpStatusCode.OK, response.StatusCode, "authenticated payout status");
}

static async Task PayoutRouteRejectsMalformedSubjectAsync()
{
    await using var app = await BuildPayoutTestAppAsync();
    using var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, "not-a-guid");

    var response = await client.PostAsJsonAsync(
        "/api/v1/mobile-money/payouts/eligibility",
        ValidEligibilityRequest());

    AssertEqual(HttpStatusCode.Unauthorized, response.StatusCode, "malformed-sub payout status");
}

static object ValidEligibilityRequest() => new
{
    sourceCountryCode = "DE",
    sourceCurrency = "EUR",
    destinationCountryCode = "CM",
    destinationCurrency = "XAF",
    operatorCode = "MTN"
};

static async Task<WebApplication> BuildPayoutTestAppAsync()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
            options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
        })
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
            TestAuthenticationHandler.Scheme,
            _ => { });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IMobileMoneyPayoutEligibilityPolicy, AlwaysEligiblePolicy>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapMobileMoneyPayoutEligibility();

    await app.StartAsync();
    return app;
}

static JwtBearerOptions BuildOptions(Dictionary<string, string?> values)
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(values)
        .Build();
    var services = new ServiceCollection();
    services.AddMobileMoneyJwtAuthentication(configuration);

    using var provider = services.BuildServiceProvider();
    return provider
        .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
        .Get(JwtBearerDefaults.AuthenticationScheme);
}

static void AssertTrue(bool value, string name)
{
    if (!value)
    {
        throw new InvalidOperationException($"{name} must be true");
    }
}

static void AssertFalse(bool value, string name)
{
    if (value)
    {
        throw new InvalidOperationException($"{name} must be false");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name} expected '{expected}' but was '{actual}'");
    }
}

sealed class AlwaysEligiblePolicy : IMobileMoneyPayoutEligibilityPolicy
{
    public Task<MobileMoneyPayoutEligibilityResult> EvaluateAsync(
        MobileMoneyPayoutCorridor corridor,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MobileMoneyPayoutEligibilityResult.Eligible());
    }
}

sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "TestMobileMoney";
    public const string SubjectHeader = "X-Test-Subject";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeader, out var subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", subject.ToString()) },
            Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
