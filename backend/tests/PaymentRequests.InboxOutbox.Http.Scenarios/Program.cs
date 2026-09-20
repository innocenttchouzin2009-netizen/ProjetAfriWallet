using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Infrastructure;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var userA = Guid.NewGuid();
var userB = Guid.NewGuid();
var walletA = WalletId.From(Guid.NewGuid());
var referenceA = RecipientReference.FromAfWalId("payer.one");
var repository = new RecordingQueryRepository();
var walletReader = new FixedOwnedWalletReader(new Dictionary<Guid, IReadOnlyCollection<WalletId>>
{
    [userA] = [walletA]
});
var referenceReader = new FixedOwnedReferenceReader(new Dictionary<Guid, IReadOnlyCollection<RecipientReference>>
{
    [userA] = [referenceA]
});

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services
    .AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPaymentRequestQueryRepository>(repository);
builder.Services.AddSingleton<IPaymentRequestOwnedWalletReader>(walletReader);
builder.Services.AddSingleton<IPaymentRequestOwnedRecipientReferenceReader>(referenceReader);
builder.Services.AddScoped<AuthorizedPaymentRequestQueryService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapPaymentRequestInboxOutboxEndpoints();
await app.StartAsync();

var anonymous = app.GetTestClient();
var clientA = CreateClient(app, userA);
var clientB = CreateClient(app, userB);

await RunAsync("inbox requires authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/payment-requests/inbox");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("inbox derives scope from JWT sub and applies server query controls", async () =>
{
    var response = await clientA.GetAsync("/api/v1/payment-requests/inbox?page=1&pageSize=5&status=pending,accepted&order=oldest");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var page = await response.Content.ReadFromJsonAsync<PaymentRequestInboxOutboxPageResponse>();
    Assert(page is not null && page.PageNumber == 1 && page.PageSize == 5, "Inbox page controls were not preserved.");
    Assert(repository.LastReceived is not null, "Inbox repository query was not issued.");
    Assert(repository.LastReceived!.RecipientWalletIds.SequenceEqual([walletA]), "Inbox must use only wallets owned by authenticated sub.");
    Assert(repository.LastReceived.RecipientReferences.SequenceEqual([referenceA]), "Inbox must use only recipient references owned by authenticated sub.");
    Assert(repository.LastReceived.Order == PaymentRequestTemporalOrder.OldestFirst, "Inbox order must be server parsed.");
    Assert(repository.LastReceived.Statuses is not null && repository.LastReceived.Statuses.Count == 2, "Inbox statuses must be server parsed.");
    var body = await response.Content.ReadAsStringAsync();
    Assert(!body.Contains("payer.one", StringComparison.Ordinal), "HTTP response must not leak AfWal ID/QR values.");
});

await RunAsync("outbox derives requester wallets from JWT sub", async () =>
{
    var response = await clientA.GetAsync("/api/v1/payment-requests/outbox?status=paid&order=newest");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    Assert(repository.LastSent is not null, "Outbox repository query was not issued.");
    Assert(repository.LastSent!.RequesterWalletIds.SequenceEqual([walletA]), "Outbox must use only wallets owned by authenticated sub.");
    Assert(repository.LastSent.Order == PaymentRequestTemporalOrder.NewestFirst, "Outbox order must be server parsed.");
    Assert(repository.LastSent.Statuses?.Single() == PaymentRequestStatus.Paid, "Outbox status must be server parsed.");
});

await RunAsync("invalid pagination is rejected before repository access", async () =>
{
    var before = repository.TotalCalls;
    var response = await clientA.GetAsync("/api/v1/payment-requests/inbox?pageSize=101");
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    Assert(repository.TotalCalls == before, "Invalid pagination must not reach repository.");
});

await RunAsync("invalid status and order are rejected", async () =>
{
    var statusResponse = await clientA.GetAsync("/api/v1/payment-requests/inbox?status=unknown");
    Assert(statusResponse.StatusCode == HttpStatusCode.BadRequest, "Unsupported status must return 400.");
    var orderResponse = await clientA.GetAsync("/api/v1/payment-requests/outbox?order=random");
    Assert(orderResponse.StatusCode == HttpStatusCode.BadRequest, "Unsupported order must return 400.");
});

await RunAsync("user without owned scopes receives empty inbox/outbox without widened reads", async () =>
{
    var before = repository.TotalCalls;
    var inbox = await clientB.GetFromJsonAsync<PaymentRequestInboxOutboxPageResponse>("/api/v1/payment-requests/inbox");
    var outbox = await clientB.GetFromJsonAsync<PaymentRequestInboxOutboxPageResponse>("/api/v1/payment-requests/outbox");
    Assert(inbox is not null && inbox.TotalCount == 0 && inbox.Items.Count == 0, "Empty-scope inbox must be empty.");
    Assert(outbox is not null && outbox.TotalCount == 0 && outbox.Items.Count == 0, "Empty-scope outbox must be empty.");
    Assert(repository.TotalCalls == before, "Empty ownership scopes must not query broader data.");
});

await RunAsync("composition wires concrete query and ownership adapters", () =>
{
    var services = new ServiceCollection();
    services.AddPaymentRequests("Data Source=:memory:");
    Assert(services.Any(x => x.ServiceType == typeof(IPaymentRequestQueryRepository) && x.ImplementationType == typeof(EfPaymentRequestRepository)),
        "Composition must wire IPaymentRequestQueryRepository to EfPaymentRequestRepository.");
    Assert(services.Any(x => x.ServiceType == typeof(IPaymentRequestOwnedWalletReader) && x.ImplementationType == typeof(WalletRegistryOwnedWalletReader)),
        "Composition must wire owned wallet reader.");
    Assert(services.Any(x => x.ServiceType == typeof(IPaymentRequestOwnedRecipientReferenceReader) && x.ImplementationType == typeof(AuthoritativeRecipientReferenceReader)),
        "Composition must wire authoritative recipient reference reader.");
    Assert(services.Any(x => x.ServiceType == typeof(AuthorizedPaymentRequestQueryService)),
        "Composition must wire authorized inbox/outbox query service.");
    return Task.CompletedTask;
});

Console.WriteLine("AFW-BE-REQUEST-INBOX-1 protected HTTP inbox/outbox scenarios: PASS");
await app.DisposeAsync();

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
    if (!condition) throw new InvalidOperationException(message);
}

sealed class RecordingQueryRepository : IPaymentRequestQueryRepository
{
    public ReceivedPaymentRequestsQuery? LastReceived { get; private set; }
    public SentPaymentRequestsQuery? LastSent { get; private set; }
    public int TotalCalls { get; private set; }

    public Task<PaymentRequestQueryPage> ListReceivedAsync(ReceivedPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastReceived = query;
        TotalCalls++;
        return Task.FromResult(Page(query.Page));
    }

    public Task<PaymentRequestQueryPage> ListSentAsync(SentPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastSent = query;
        TotalCalls++;
        return Task.FromResult(Page(query.Page));
    }

    private static PaymentRequestQueryPage Page(PaymentRequestPageRequest page) => new(
        [new PaymentRequestQueryItem(
            PaymentRequestId.From(Guid.NewGuid()),
            WalletId.From(Guid.NewGuid()),
            RecipientReferenceKind.AfWalId,
            Currency.Create("XAF"),
            2_500,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            null,
            DateTimeOffset.UtcNow,
            PaymentRequestStatus.Pending,
            null,
            null,
            null,
            null)],
        page.PageNumber,
        page.PageSize,
        1,
        false);
}

sealed class FixedOwnedWalletReader(IReadOnlyDictionary<Guid, IReadOnlyCollection<WalletId>> values)
    : IPaymentRequestOwnedWalletReader
{
    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(userId, out var wallets) ? wallets : (IReadOnlyCollection<WalletId>)Array.Empty<WalletId>());
    }
}

sealed class FixedOwnedReferenceReader(IReadOnlyDictionary<Guid, IReadOnlyCollection<RecipientReference>> values)
    : IPaymentRequestOwnedRecipientReferenceReader
{
    public Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(userId, out var references) ? references : (IReadOnlyCollection<RecipientReference>)Array.Empty<RecipientReference>());
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
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
