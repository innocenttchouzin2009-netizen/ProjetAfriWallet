using System.Net;
using System.Net.Http.Json;
using AfriWallet.P2P.Infrastructure;
using IdentityService.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

const string activeAfWalId = "public.user";
var ownerId = Guid.NewGuid();

await using var app = await BuildAppAsync(
    new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        [activeAfWalId] = ownerId
    });

var client = app.GetTestClient();

await RunAsync("active AfWal ID is publicly readable", async () =>
{
    var response = await client.GetAsync($"/api/v1/identity/afwal-id/{activeAfWalId}");

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var model = await response.Content.ReadFromJsonAsync<PublicAfWalIdReadModel>();

    Assert(model is not null, "Public AfWal ID response is required.");
    Assert(model!.AfWalId == activeAfWalId, "Response must return the authoritative AfWal ID.");
});

await RunAsync("public response does not expose owner id", async () =>
{
    var response = await client.GetAsync($"/api/v1/identity/afwal-id/{activeAfWalId}");
    var payload = await response.Content.ReadAsStringAsync();

    Assert(!payload.Contains(ownerId.ToString(), StringComparison.OrdinalIgnoreCase),
        "Public response must not expose the internal owner id.");
});

await RunAsync("unknown AfWal ID returns not found", async () =>
{
    var response = await client.GetAsync("/api/v1/identity/afwal-id/missing.user");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("invalid AfWal ID returns not found without leaking validation details", async () =>
{
    var response = await client.GetAsync("/api/v1/identity/afwal-id/bad%20id");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Console.WriteLine("Identity public AfWal ID scenarios passed.");

static async Task<WebApplication> BuildAppAsync(IReadOnlyDictionary<string, Guid> entries)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.Services.AddSingleton<IAfWalIdentityDirectory>(new FixedAfWalIdentityDirectory(entries));
    builder.Services.AddScoped<PublicAfWalIdReadService>();

    var app = builder.Build();
    app.MapPublicAfWalIdEndpoints();
    await app.StartAsync();
    return app;
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

sealed class FixedAfWalIdentityDirectory(IReadOnlyDictionary<string, Guid> entries) : IAfWalIdentityDirectory
{
    public Task<Guid?> ResolveOwnerIdAsync(
        string afWalId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(entries.TryGetValue(afWalId, out var ownerId)
            ? (Guid?)ownerId
            : null);
    }
}
