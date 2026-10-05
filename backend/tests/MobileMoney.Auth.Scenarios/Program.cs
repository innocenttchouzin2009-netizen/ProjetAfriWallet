using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MobileMoney.Production.Authentication;

var tests = new (string Name, Action Run)[]
{
    ("configured issuer, audience and signing key are reused", ConfiguredValuesAreReused),
    ("JWT validation is strict and claim mapping stays disabled", ValidationIsStrict),
    ("AFW_AUTH_JWT_SIGNING_KEY is supported as fallback", EnvironmentSigningKeyIsSupported),
    ("missing signing key fails closed", MissingSigningKeyFailsClosed)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        test.Run();
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
