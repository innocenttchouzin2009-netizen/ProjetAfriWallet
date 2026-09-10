using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using IdentityService.Api.Balance;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await BalanceHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var authenticated = CreateClient(fixture.App, Guid.NewGuid());
var accountId = Guid.NewGuid();

await RunAsync("balance read requires authentication", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/balances/{accountId}/XAF");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated empty ledger returns zero projection", async () =>
{
    var response = await authenticated.GetAsync($"/api/v1/balances/{accountId}/xaf");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();
    Assert(balance is not null, "Balance response is required.");
    Assert(balance!.AccountId == accountId, "Account id mismatch.");
    Assert(balance.CurrencyCode == "XAF", "Currency must be normalized.");
    Assert(balance.DebitMinor == 0 && balance.CreditMinor == 0 && balance.NetMinor == 0, "Empty ledger must project zero balance.");
});

await RunAsync("invalid currency returns 400", async () =>
{
    var response = await authenticated.GetAsync($"/api/v1/balances/{accountId}/BAD4");
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");

    var error = await response.Content.ReadFromJsonAsync<BalanceErrorResponse>();
    Assert(error is not null && error.Code == "BALANCE_VALIDATION_ERROR", "Expected BALANCE_VALIDATION_ERROR.");
});

await RunAsync("balance is projected from persisted ledger journals", async () =>
{
    var target = new AccountId(accountId);
    var counterparty = AccountId.New();
    await fixture.AddJournalAsync(Journal("XAF", target, LedgerSide.Credit, counterparty, LedgerSide.Debit, 8_000, DateTimeOffset.UtcNow.AddMinutes(-2)));
    await fixture.AddJournalAsync(Journal("XAF", target, LedgerSide.Debit, counterparty, LedgerSide.Credit, 2_500, DateTimeOffset.UtcNow.AddMinutes(-1)));
    await fixture.AddJournalAsync(Journal("EUR", target, LedgerSide.Credit, counterparty, LedgerSide.Debit, 99_000, DateTimeOffset.UtcNow));

    var response = await authenticated.GetAsync($"/api/v1/balances/{accountId}/XAF");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();
    Assert(balance is not null, "Balance response is required.");
    Assert(balance!.DebitMinor == 2_500, "Debit projection mismatch.");
    Assert(balance.CreditMinor == 8_000, "Credit projection mismatch.");
    Assert(balance.NetMinor == 5_500, "Net projection mismatch.");
});

await RunAsync("no balance write route is exposed", async () =>
{
    var path = $"/api/v1/balances/{accountId}/XAF";
    foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
    {
        using var request = new HttpRequestMessage(method, path);
        var response = await authenticated.SendAsync(request);
        Assert(response.StatusCode == HttpStatusCode.MethodNotAllowed, $"Expected 405 for {method}, got {(int)response.StatusCode}.");
    }
});

Console.WriteLine("Balance HTTP security & integration scenarios passed.");

static JournalEntry Journal(
    string currency,
    AccountId account,
    LedgerSide accountSide,
    AccountId counterparty,
    LedgerSide counterpartySide,
    long amountMinor,
    DateTimeOffset postedAtUtc) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currency,
        $"BAL-HTTP-{Guid.NewGuid():N}",
        Guid.NewGuid(),
        postedAtUtc,
        [
            new LedgerLine(account, accountSide, amountMinor),
            new LedgerLine(counterparty, counterpartySide, amountMinor)
        ]);

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

sealed class BalanceHttpFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private BalanceHttpFixture(WebApplication app, SqliteConnection connection)
    {
        App = app;
        this.connection = connection;
    }

    public WebApplication App { get; }

    public static async Task<BalanceHttpFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddScoped<IJournalRepository, EfJournalRepository>();
        builder.Services.AddScoped<ILedgerJournalReader, EfLedgerJournalReader>();
        builder.Services.AddSingleton<BalanceProjectionService>();
        builder.Services.AddScoped<LedgerBackedBalanceReadService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapBalanceEndpoints();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await db.Database.MigrateAsync();
        }

        await app.StartAsync();
        return new BalanceHttpFixture(app, connection);
    }

    public async Task AddJournalAsync(JournalEntry journalEntry)
    {
        await using var scope = App.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJournalRepository>();
        await repository.AddAsync(journalEntry);
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        await connection.DisposeAsync();
    }
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
