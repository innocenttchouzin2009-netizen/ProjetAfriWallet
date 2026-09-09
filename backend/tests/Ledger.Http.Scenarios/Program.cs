using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using IdentityService.Api.Ledger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var app = await BuildAppAsync();
var anonymous = app.GetTestClient();
var authenticated = CreateClient(app, Guid.NewGuid());

await RunAsync("all ledger routes require authentication", async () =>
{
    var correlationId = Guid.NewGuid();
    var journalId = Guid.NewGuid();

    var post = await anonymous.PostAsJsonAsync("/api/v1/ledger/journals", CreateValidRequest(correlationId));
    var getById = await anonymous.GetAsync($"/api/v1/ledger/journals/{journalId}");
    var getByCorrelation = await anonymous.GetAsync($"/api/v1/ledger/journals/by-correlation/{correlationId}");

    Assert(post.StatusCode == HttpStatusCode.Unauthorized, $"Expected POST 401, got {(int)post.StatusCode}.");
    Assert(getById.StatusCode == HttpStatusCode.Unauthorized, $"Expected GET by id 401, got {(int)getById.StatusCode}.");
    Assert(getByCorrelation.StatusCode == HttpStatusCode.Unauthorized, $"Expected GET by correlation 401, got {(int)getByCorrelation.StatusCode}.");
});

Guid createdJournalId = Guid.Empty;
var createdCorrelationId = Guid.NewGuid();

await RunAsync("authenticated posting returns 201", async () =>
{
    var response = await authenticated.PostAsJsonAsync("/api/v1/ledger/journals", CreateValidRequest(createdCorrelationId));
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");

    var journal = await response.Content.ReadFromJsonAsync<JournalEntryView>();
    Assert(journal is not null, "Journal response is required.");
    Assert(journal!.CorrelationId == createdCorrelationId, "Correlation id must be preserved.");
    Assert(journal.CurrencyCode == "XAF", "Currency must be normalized.");
    Assert(journal.Lines.Count == 2, "Journal must contain both lines.");
    createdJournalId = journal.JournalEntryId;
});

await RunAsync("duplicate correlation returns 409", async () =>
{
    var response = await authenticated.PostAsJsonAsync("/api/v1/ledger/journals", CreateValidRequest(createdCorrelationId));
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
});

await RunAsync("unbalanced journal returns 400", async () =>
{
    var response = await authenticated.PostAsJsonAsync("/api/v1/ledger/journals", new
    {
        currencyCode = "XAF",
        businessReference = "HTTP-INVALID",
        correlationId = Guid.NewGuid(),
        lines = new[]
        {
            new { accountId = Guid.NewGuid(), side = LedgerSide.Debit, amountMinor = 1500L, memo = "debit" },
            new { accountId = Guid.NewGuid(), side = LedgerSide.Credit, amountMinor = 1400L, memo = "credit" }
        }
    });

    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("journal can be read by id", async () =>
{
    var response = await authenticated.GetAsync($"/api/v1/ledger/journals/{createdJournalId}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var journal = await response.Content.ReadFromJsonAsync<JournalEntryView>();
    Assert(journal is not null && journal.JournalEntryId == createdJournalId, "Journal id lookup must return posted journal.");
});

await RunAsync("journal can be read by correlation", async () =>
{
    var response = await authenticated.GetAsync($"/api/v1/ledger/journals/by-correlation/{createdCorrelationId}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var journal = await response.Content.ReadFromJsonAsync<JournalEntryView>();
    Assert(journal is not null && journal.CorrelationId == createdCorrelationId, "Correlation lookup must return posted journal.");
});

await RunAsync("unknown journals return 404", async () =>
{
    var byId = await authenticated.GetAsync($"/api/v1/ledger/journals/{Guid.NewGuid()}");
    var byCorrelation = await authenticated.GetAsync($"/api/v1/ledger/journals/by-correlation/{Guid.NewGuid()}");
    Assert(byId.StatusCode == HttpStatusCode.NotFound, $"Expected 404 by id, got {(int)byId.StatusCode}.");
    Assert(byCorrelation.StatusCode == HttpStatusCode.NotFound, $"Expected 404 by correlation, got {(int)byCorrelation.StatusCode}.");
});

Console.WriteLine("Ledger HTTP security & integration scenarios passed.");

static object CreateValidRequest(Guid correlationId) => new
{
    currencyCode = "xaf",
    businessReference = " HTTP-POST-001 ",
    correlationId,
    lines = new[]
    {
        new { accountId = Guid.NewGuid(), side = LedgerSide.Debit, amountMinor = 1500L, memo = "debit" },
        new { accountId = Guid.NewGuid(), side = LedgerSide.Credit, amountMinor = 1500L, memo = "credit" }
    }
};

static async Task<WebApplication> BuildAppAsync()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IJournalRepository, InMemoryJournalRepository>();
    builder.Services.AddScoped<LedgerPostingApplicationService>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapLedgerEndpoints();
    await app.StartAsync();
    return app;
}

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

sealed class InMemoryJournalRepository : IJournalRepository
{
    private readonly Dictionary<Guid, JournalEntry> journals = new();

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(journals.Values.Any(journal => journal.CorrelationId == correlationId));

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        journals.Add(journalEntry.Id.Value, journalEntry);
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        journals.TryGetValue(journalEntryId.Value, out var journal);
        return Task.FromResult(journal);
    }

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(journals.Values.SingleOrDefault(journal => journal.CorrelationId == correlationId));
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

        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
