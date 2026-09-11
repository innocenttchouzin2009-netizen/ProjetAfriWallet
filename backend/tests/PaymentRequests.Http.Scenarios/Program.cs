using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await PaymentRequestHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var ownerClient = CreateClient(fixture.App, fixture.OwnerId);
var foreignClient = CreateClient(fixture.App, fixture.ForeignOwnerId);

await RunAsync("payment request creation requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync("/api/v1/payment-requests/", fixture.Request(Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign requester wallet is hidden with 404", async () =>
{
    var request = fixture.Request(Guid.NewGuid()) with { RequesterWalletId = fixture.ForeignWalletId };
    var response = await ownerClient.PostAsJsonAsync("/api/v1/payment-requests/", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("unsupported recipient kind returns 400", async () =>
{
    var request = fixture.Request(Guid.NewGuid()) with { RecipientKind = "phone" };
    var response = await ownerClient.PostAsJsonAsync("/api/v1/payment-requests/", request);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("unknown recipient returns 404", async () =>
{
    fixture.Resolver.Next = null;
    var response = await ownerClient.PostAsJsonAsync("/api/v1/payment-requests/", fixture.Request(Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    fixture.Resolver.Next = WalletId.From(fixture.PayerWalletId);
});

PaymentRequestHttpResponse? createdResponse = null;
var correlationId = Guid.NewGuid();
await RunAsync("owned requester creates pending request without transfer execution", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/payment-requests/", fixture.Request(correlationId));
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    createdResponse = await response.Content.ReadFromJsonAsync<PaymentRequestHttpResponse>();
    Assert(createdResponse is not null, "Created response is required.");
    Assert(createdResponse!.RequesterWalletId == fixture.RequesterWalletId, "Requester wallet mismatch.");
    Assert(createdResponse.Status == "Pending", "New request must be pending.");
    Assert(createdResponse.TransferId is null, "Creation must not execute a transfer.");
    Assert(fixture.Repository.AddCalls == 1, "Creation must persist once.");
});

await RunAsync("same correlation is idempotent", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/payment-requests/", fixture.Request(correlationId));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var replay = await response.Content.ReadFromJsonAsync<PaymentRequestHttpResponse>();
    Assert(replay?.Id == createdResponse?.Id, "Idempotent replay must return the same request.");
    Assert(fixture.Repository.AddCalls == 1, "Idempotent replay must not persist twice.");
});

await RunAsync("requester can read own payment request", async () =>
{
    var response = await ownerClient.GetAsync($"/api/v1/payment-requests/{createdResponse!.Id}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
});

await RunAsync("foreign user cannot read requester payment request", async () =>
{
    var response = await foreignClient.GetAsync($"/api/v1/payment-requests/{createdResponse!.Id}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("composition wires SQLite repository and P2P recipient resolver", () =>
{
    var services = new ServiceCollection();
    services.AddPaymentRequests("Data Source=:memory:");
    Assert(services.Any(x => x.ServiceType == typeof(IPaymentRequestRepository) && x.ImplementationType == typeof(EfPaymentRequestRepository)),
        "Composition must wire EF payment request repository.");
    Assert(services.Any(x => x.ServiceType == typeof(IPaymentRequestRecipientResolver) && x.ImplementationType == typeof(P2PRecipientResolver)),
        "Composition must wire P2P recipient resolver.");
    return Task.CompletedTask;
});

Console.WriteLine("AFW-BE-REQUEST-1 protected payment request HTTP scenarios: PASS");

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

sealed class PaymentRequestHttpFixture : IAsyncDisposable
{
    private PaymentRequestHttpFixture(WebApplication app, InMemoryRepository repository, FixedResolver resolver,
        Guid ownerId, Guid foreignOwnerId, Guid requesterWalletId, Guid foreignWalletId, Guid payerWalletId)
    {
        App = app;
        Repository = repository;
        Resolver = resolver;
        OwnerId = ownerId;
        ForeignOwnerId = foreignOwnerId;
        RequesterWalletId = requesterWalletId;
        ForeignWalletId = foreignWalletId;
        PayerWalletId = payerWalletId;
    }

    public WebApplication App { get; }
    public InMemoryRepository Repository { get; }
    public FixedResolver Resolver { get; }
    public Guid OwnerId { get; }
    public Guid ForeignOwnerId { get; }
    public Guid RequesterWalletId { get; }
    public Guid ForeignWalletId { get; }
    public Guid PayerWalletId { get; }

    public CreatePaymentRequestHttpRequest Request(Guid correlationId) =>
        new(RequesterWalletId, "afwal-id", "payer.one", "XAF", 2_500, correlationId, DateTimeOffset.UtcNow.AddHours(1));

    public static async Task<PaymentRequestHttpFixture> CreateAsync()
    {
        var ownerId = Guid.NewGuid();
        var foreignOwnerId = Guid.NewGuid();
        var payerOwnerId = Guid.NewGuid();
        var requesterWalletId = Guid.NewGuid();
        var foreignWalletId = Guid.NewGuid();
        var payerWalletId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var walletRepository = new InMemoryWalletRepository([
            Wallet.Create(WalletId.From(requesterWalletId), ownerId, Currency.Create("XAF"), null, now),
            Wallet.Create(WalletId.From(foreignWalletId), foreignOwnerId, Currency.Create("XAF"), null, now),
            Wallet.Create(WalletId.From(payerWalletId), payerOwnerId, Currency.Create("XAF"), null, now)
        ]);
        var repository = new InMemoryRepository();
        var resolver = new FixedResolver(WalletId.From(payerWalletId));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IWalletRepository>(walletRepository);
        builder.Services.AddSingleton<IPaymentRequestRepository>(repository);
        builder.Services.AddSingleton<IPaymentRequestRecipientResolver>(resolver);
        builder.Services.AddSingleton<PaymentRequestApplicationService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestEndpoints();
        await app.StartAsync();

        return new PaymentRequestHttpFixture(app, repository, resolver, ownerId, foreignOwnerId, requesterWalletId, foreignWalletId, payerWalletId);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryRepository : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = new();
    private readonly Dictionary<Guid, PaymentRequest> byCorrelation = new();
    public int AddCalls { get; private set; }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.Add(request.Id.Value, request);
        byCorrelation.Add(request.CorrelationId, request);
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.TryGetValue(id.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        return Task.CompletedTask;
    }
}

sealed class FixedResolver(WalletId? next) : IPaymentRequestRecipientResolver
{
    public WalletId? Next { get; set; } = next;
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Next);
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(x => x.Id.Value);
    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));
    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default) { values[wallet.Id.Value] = wallet; return Task.CompletedTask; }
    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(walletId.Value, out var wallet) ? wallet : null);
    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(x => x.OwnerId == ownerId).ToArray());
    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) { values[wallet.Id.Value] = wallet; return Task.CompletedTask; }
}

sealed class HeaderTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
