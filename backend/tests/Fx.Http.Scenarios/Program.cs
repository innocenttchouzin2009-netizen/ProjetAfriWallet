using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using IdentityService.Api.Fx;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await FxHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var authenticated = CreateClient(fixture.App, Guid.NewGuid());

await RunAsync("fx quote requires authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/fx/quotes/EUR/XAF");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("configured provider returns normalized quote", async () =>
{
    var response = await authenticated.GetAsync("/api/v1/fx/quotes/eur/xaf");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var quote = await response.Content.ReadFromJsonAsync<FxQuoteResponse>();
    Assert(quote is not null, "FX quote response is required.");
    Assert(quote!.SourceCurrencyCode == "EUR", "Source currency must normalize to EUR.");
    Assert(quote.TargetCurrencyCode == "XAF", "Target currency must normalize to XAF.");
    Assert(quote.Rate == 655.957m, $"Expected rate 655.957, got {quote.Rate}.");
    Assert(quote.QuotedAtUtc == FxHttpFixture.FixedNow, "Quote timestamp must come from the configured provider clock.");
});

await RunAsync("invalid pair returns 400", async () =>
{
    var response = await authenticated.GetAsync("/api/v1/fx/quotes/EUR/EUR");
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");

    var error = await response.Content.ReadFromJsonAsync<FxErrorResponse>();
    Assert(error is not null && error.Code == FxQuoteErrorCode.ValidationError, "Expected FX_QUOTE_VALIDATION_ERROR.");
});

await RunAsync("unknown configured pair returns 404", async () =>
{
    var response = await authenticated.GetAsync("/api/v1/fx/quotes/EUR/USD");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");

    var error = await response.Content.ReadFromJsonAsync<FxErrorResponse>();
    Assert(error is not null && error.Code == FxQuoteErrorCode.QuoteUnavailable, "Expected FX_QUOTE_UNAVAILABLE.");
});

await RunAsync("no fx write route is exposed", async () =>
{
    const string path = "/api/v1/fx/quotes/EUR/XAF";
    foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
    {
        using var request = new HttpRequestMessage(method, path);
        var response = await authenticated.SendAsync(request);
        Assert(response.StatusCode == HttpStatusCode.MethodNotAllowed, $"Expected 405 for {method}, got {(int)response.StatusCode}.");
    }
});

Console.WriteLine("FX HTTP security & integration scenarios passed.");

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FxHttpFixture : IAsyncDisposable
{
    public static readonly DateTimeOffset FixedNow = new(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

    private FxHttpFixture(WebApplication app) => App = app;

    public WebApplication App { get; }

    public static async Task<FxHttpFixture> CreateAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var provider = new ConfiguredFxQuoteProvider(
            [new ConfiguredFxRate("eur", "xaf", 655.957m)],
            new FixedTimeProvider(FixedNow));
        builder.Services.AddSingleton<IFxQuoteProvider>(provider);
        builder.Services.AddScoped<FxQuoteApplicationService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFxEndpoints();
        await app.StartAsync();
        return new FxHttpFixture(app);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
