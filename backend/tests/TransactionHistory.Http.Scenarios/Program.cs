using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.TransactionHistory.Application;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.TransactionHistory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
var walletId = WalletId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
var firstId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
var secondId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
var firstAt = new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero);
var secondAt = firstAt.AddMinutes(-1);

await using var app = await BuildAppAsync(userId, walletId, firstId, secondId, firstAt, secondAt);
var anonymous = app.GetTestClient();
var authenticated = CreateClient(app, userId);
var noSub = CreateNoSubClient(app);

await RunAsync("transaction history requires authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/transactions");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated principal without sub is rejected", async () =>
{
    var response = await noSub.GetAsync("/api/v1/transactions");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransactionHistoryErrorResponse>();
    Assert(error?.Code == TransactionHistoryHttpErrorCode.Unauthorized, "Stable unauthorized code is required.");
});

string? nextCursor = null;

await RunAsync("first page returns HTTP DTO and opaque cursor", async () =>
{
    var response = await authenticated.GetAsync("/api/v1/transactions?limit=1");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var page = await response.Content.ReadFromJsonAsync<TransactionHistoryPageResponse>();
    Assert(page is { Items.Count: 1 }, "First page must contain exactly one item.");
    Assert(page!.Items[0].TransactionId == firstId, "First transaction id mismatch.");
    Assert(page.Items[0].WalletId == walletId.Value, "Wallet id mismatch.");
    Assert(page.Items[0].Direction == "Incoming", "Direction must be serialized as a stable string.");
    Assert(page.Items[0].Status == "Completed", "Status must be serialized as a stable string.");
    Assert(!string.IsNullOrWhiteSpace(page.NextCursor), "First page must return a next cursor.");

    nextCursor = page.NextCursor;
    Assert(!nextCursor!.Contains(firstId.ToString("N"), StringComparison.OrdinalIgnoreCase), "Cursor must not expose the transaction id in clear text.");
    Assert(!nextCursor.Contains("|", StringComparison.Ordinal), "Cursor must not expose its internal tuple format.");
});

await RunAsync("opaque cursor round trips to the next stable page", async () =>
{
    var response = await authenticated.GetAsync($"/api/v1/transactions?limit=1&cursor={Uri.EscapeDataString(nextCursor!)}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var page = await response.Content.ReadFromJsonAsync<TransactionHistoryPageResponse>();
    Assert(page is { Items.Count: 1 }, "Second page must contain exactly one item.");
    Assert(page!.Items[0].TransactionId == secondId, "Cursor must resume after the first transaction.");
    Assert(page.NextCursor is null, "Last page must not return another cursor.");
});

await RunAsync("invalid cursor returns stable validation error", async () =>
{
    var response = await authenticated.GetAsync("/api/v1/transactions?cursor=not-a-valid-cursor");
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");

    var error = await response.Content.ReadFromJsonAsync<TransactionHistoryErrorResponse>();
    Assert(error?.Code == TransactionHistoryHttpErrorCode.ValidationError, "Stable validation code is required.");
});

await RunAsync("page limits outside contract are rejected", async () =>
{
    foreach (var limit in new[] { 0, TransactionHistoryPageRequest.MaximumLimit + 1 })
    {
        var response = await authenticated.GetAsync($"/api/v1/transactions?limit={limit}");
        Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 for limit {limit}, got {(int)response.StatusCode}.");
    }
});

await RunAsync("authenticated user id is propagated to ownership authorization", async () =>
{
    await authenticated.GetAsync("/api/v1/transactions");
    await using var scope = app.Services.CreateAsyncScope();
    var owned = (StubOwnedWalletReader)scope.ServiceProvider.GetRequiredService<ITransactionHistoryOwnedWalletReader>();
    Assert(owned.LastUserId == userId, "HTTP adapter must authorize history with the authenticated user id.");
});

Console.WriteLine("Transaction history HTTP scenarios passed: 6/6");

static async Task<WebApplication> BuildAppAsync(
    Guid userId,
    WalletId walletId,
    Guid firstId,
    Guid secondId,
    DateTimeOffset firstAt,
    DateTimeOffset secondAt)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<ITransactionHistoryOwnedWalletReader>(
        new StubOwnedWalletReader(userId, walletId));
    builder.Services.AddSingleton<ITransactionHistoryReader>(
        new StubHistoryReader(walletId, firstId, secondId, firstAt, secondAt));
    builder.Services.AddScoped<AuthorizedTransactionHistoryQueryService>();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapTransactionHistoryEndpoints();
    await app.StartAsync();
    return app;
}

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static HttpClient CreateNoSubClient(WebApplication app)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-No-Sub", "1");
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

sealed class StubOwnedWalletReader(Guid expectedUserId, WalletId walletId)
    : ITransactionHistoryOwnedWalletReader
{
    public Guid? LastUserId { get; private set; }

    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastUserId = userId;
        Assert(userId == expectedUserId, "Unexpected authenticated user id.");
        return Task.FromResult<IReadOnlyCollection<WalletId>>([walletId]);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

sealed class StubHistoryReader(
    WalletId walletId,
    Guid firstId,
    Guid secondId,
    DateTimeOffset firstAt,
    DateTimeOffset secondAt) : ITransactionHistoryReader
{
    public Task<TransactionHistoryPage> ReadAsync(
        IReadOnlyCollection<WalletId> walletIds,
        TransactionHistoryPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!walletIds.Contains(walletId))
        {
            throw new InvalidOperationException("Authorized wallet set mismatch.");
        }

        if (page.Cursor is null)
        {
            var first = Item(firstId, firstAt, TransactionHistoryDirection.Incoming);
            var cursor = new TransactionHistoryCursor(firstAt, firstId);
            return Task.FromResult(new TransactionHistoryPage([first], cursor));
        }

        if (page.Cursor.Value.OccurredAtUtc != firstAt ||
            page.Cursor.Value.TransactionId != firstId)
        {
            throw new InvalidOperationException("Decoded cursor mismatch.");
        }

        var second = Item(secondId, secondAt, TransactionHistoryDirection.Outgoing);
        return Task.FromResult(new TransactionHistoryPage([second], null));
    }

    private TransactionHistoryItem Item(
        Guid id,
        DateTimeOffset occurredAtUtc,
        TransactionHistoryDirection direction) =>
        new(
            id,
            walletId,
            2500,
            "EUR",
            direction,
            TransactionHistoryStatus.Completed,
            occurredAtUtc,
            $"TX-{id:N}",
            "Counterparty");
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-No-Sub"))
        {
            var noSubIdentity = new ClaimsIdentity([new Claim("name", "test")], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(noSubIdentity), Scheme.Name)));
        }

        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) ||
            !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
