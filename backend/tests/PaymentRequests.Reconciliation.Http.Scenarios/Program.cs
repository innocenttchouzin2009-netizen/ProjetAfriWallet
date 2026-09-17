using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await ReconciliationFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var owner = CreateClient(fixture.App, fixture.OwnerId);
var foreign = CreateClient(fixture.App, fixture.ForeignOwnerId);

await RunAsync("reconciliation requires authentication", async () =>
{
    var response = await anonymous.PostAsync($"/api/v1/payment-requests/{fixture.Accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign requester is hidden with 404", async () =>
{
    var response = await foreign.PostAsync($"/api/v1/payment-requests/{fixture.Accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("missing request returns 404", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{Guid.NewGuid()}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("pending request is not eligible", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{fixture.Pending.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
});

await RunAsync("accepted request without transfer stays accepted", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{fixture.NoReceipt.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    Assert(fixture.NoReceipt.Status == PaymentRequestStatus.Accepted, "Missing receipt must not mark request paid.");
});

await RunAsync("matching certified receipt reconciles accepted request", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{fixture.Accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PaymentRequestReconciliationHttpResponse>();
    Assert(body is not null, "Reconciliation response is required.");
    Assert(body!.ReconciliationStatus == nameof(PaymentRequestRecoveryStatus.Reconciled), "Expected Reconciled status.");
    Assert(body.Status == nameof(PaymentRequestStatus.Paid), "Request must become Paid.");
    Assert(body.TransferId == fixture.TransferId, "Transfer id must match certified receipt.");
    Assert(fixture.Accepted.Status == PaymentRequestStatus.Paid, "Persisted request must be Paid.");
    Assert(fixture.MutationStore.UpdateCount == 1, "Paid lifecycle mutation must be emitted exactly once.");
    Assert(fixture.MutationStore.LastEvent?.Kind == PaymentRequestLifecycleEventKind.Paid, "Paid lifecycle event is required.");
});

await RunAsync("reconciliation replay is idempotent", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{fixture.Accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PaymentRequestReconciliationHttpResponse>();
    Assert(body?.ReconciliationStatus == nameof(PaymentRequestRecoveryStatus.AlreadyPaid), "Expected AlreadyPaid on replay.");
    Assert(fixture.MutationStore.UpdateCount == 1, "Replay must not emit a second Paid lifecycle mutation.");
});

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-2 protected reconciliation scenarios: PASS");

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

sealed class ReconciliationFixture : IAsyncDisposable
{
    private ReconciliationFixture(
        WebApplication app,
        Guid ownerId,
        Guid foreignOwnerId,
        PaymentRequest accepted,
        PaymentRequest pending,
        PaymentRequest noReceipt,
        Guid transferId,
        RecordingLifecycleMutationStore mutationStore)
    {
        App = app;
        OwnerId = ownerId;
        ForeignOwnerId = foreignOwnerId;
        Accepted = accepted;
        Pending = pending;
        NoReceipt = noReceipt;
        TransferId = transferId;
        MutationStore = mutationStore;
    }

    public WebApplication App { get; }
    public Guid OwnerId { get; }
    public Guid ForeignOwnerId { get; }
    public PaymentRequest Accepted { get; }
    public PaymentRequest Pending { get; }
    public PaymentRequest NoReceipt { get; }
    public Guid TransferId { get; }
    public RecordingLifecycleMutationStore MutationStore { get; }

    public static async Task<ReconciliationFixture> CreateAsync()
    {
        var ownerId = Guid.NewGuid();
        var foreignOwnerId = Guid.NewGuid();
        var requesterWallet = WalletId.From(Guid.NewGuid());
        var payerWallet = WalletId.From(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 17, 14, 0, 0, TimeSpan.Zero);
        var currency = Currency.Create("EUR");
        var payerReference = RecipientReference.FromAfWalId("payer.one");

        PaymentRequest NewRequest() => PaymentRequest.Create(
            requesterWallet,
            payerReference,
            currency,
            5_000,
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(2));

        var accepted = NewRequest();
        accepted.Accept(payerWallet, createdAt.AddMinutes(1));
        var pending = NewRequest();
        var noReceipt = NewRequest();
        noReceipt.Accept(payerWallet, createdAt.AddMinutes(1));

        var repository = new InMemoryRepository([accepted, pending, noReceipt]);
        var ownership = new FixedOwnershipReader(requesterWallet, ownerId);
        var transferId = Guid.NewGuid();
        var receipt = new PaymentRequestPaymentReceipt(
            transferId,
            payerWallet.Value,
            requesterWallet.Value,
            accepted.AmountMinor,
            accepted.Id.Value,
            createdAt.AddMinutes(2));
        var reconciliation = new FixedReconciliationPort(new Dictionary<Guid, PaymentRequestPaymentReceipt>
        {
            [accepted.Id.Value] = receipt
        });
        var mutationStore = new RecordingLifecycleMutationStore(repository);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPaymentRequestRepository>(repository);
        builder.Services.AddSingleton<IPaymentRequestWalletOwnershipReader>(ownership);
        builder.Services.AddSingleton<IPaymentRequestReconciliationPort>(reconciliation);
        builder.Services.AddSingleton<IPaymentRequestLifecycleMutationStore>(mutationStore);
        builder.Services.AddScoped<PaymentRequestRecoveryService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestReconciliationEndpoints();
        await app.StartAsync();

        return new ReconciliationFixture(app, ownerId, foreignOwnerId, accepted, pending, noReceipt, transferId, mutationStore);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryRepository(IEnumerable<PaymentRequest> requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = requests.ToDictionary(x => x.Id.Value);

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(id.Value, out var value) ? value : null);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.Values.SingleOrDefault(x => x.CorrelationId == correlationId));
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        return Task.CompletedTask;
    }
}

sealed class FixedOwnershipReader(WalletId walletId, Guid ownerId) : IPaymentRequestWalletOwnershipReader
{
    public Task<bool> IsOwnedByAsync(WalletId candidateWalletId, Guid candidateOwnerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(candidateWalletId == walletId && candidateOwnerId == ownerId);
    }
}

sealed class FixedReconciliationPort(IReadOnlyDictionary<Guid, PaymentRequestPaymentReceipt> receipts)
    : IPaymentRequestReconciliationPort
{
    public Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(receipts.TryGetValue(correlationId, out var receipt) ? receipt : null);
    }
}

sealed class RecordingLifecycleMutationStore(IPaymentRequestRepository repository) : IPaymentRequestLifecycleMutationStore
{
    public int UpdateCount { get; private set; }
    public PaymentRequestLifecycleEvent? LastEvent { get; private set; }

    public Task AddAsync(PaymentRequest request, PaymentRequestLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public async Task UpdateAsync(PaymentRequest request, PaymentRequestLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpdateCount++;
        LastEvent = lifecycleEvent;
        await repository.UpdateAsync(request, cancellationToken);
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
