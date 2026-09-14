using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using IdentityService.Api.Transfer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

// Composition must reuse the certified Transfer receipt reader and add only a reconciliation reader/service.
var composition = new ServiceCollection();
composition.AddInternalTransferModule(new ConfigurationBuilder().Build());
composition.AddPaymentRequests("Data Source=:memory:");
Assert(composition.Any(x => x.ServiceType == typeof(ITransferReceiptReader)),
    "Certified ITransferReceiptReader must remain registered.");
Assert(composition.Any(x => x.ServiceType == typeof(IPaymentRequestPaymentReceiptReader) &&
                            x.ImplementationType == typeof(TransferCorrelationPaymentReceiptReader)),
    "Payment Request reconciliation must adapt the certified Transfer receipt reader.");
Assert(composition.Any(x => x.ServiceType == typeof(PaymentRequestReconciliationService)),
    "PaymentRequestReconciliationService must be registered.");
Assert(composition.Count(x => x.ServiceType == typeof(IPaymentRequestPaymentPort)) == 1,
    "Reconciliation wiring must not introduce a second payment execution port.");

var now = new DateTimeOffset(2026, 9, 14, 16, 0, 0, TimeSpan.Zero);
var ownerId = Guid.NewGuid();
var foreignOwnerId = Guid.NewGuid();
var payerOwnerId = Guid.NewGuid();
var requesterWallet = WalletId.From(Guid.NewGuid());
var foreignRequesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var currency = Currency.Create("XAF");
var payerRef = RecipientReference.FromAfWalId("payer.reconcile");

PaymentRequest NewRequest(WalletId requester) => PaymentRequest.Create(
    requester,
    payerRef,
    currency,
    2_500,
    Guid.NewGuid(),
    now,
    now.AddHours(1));

var accepted = NewRequest(requesterWallet);
accepted.Accept(payerWallet, now.AddMinutes(1));
var missingTransfer = NewRequest(requesterWallet);
missingTransfer.Accept(payerWallet, now.AddMinutes(1));
var pending = NewRequest(requesterWallet);
var foreign = NewRequest(foreignRequesterWallet);
foreign.Accept(payerWallet, now.AddMinutes(1));

var repository = new InMemoryPaymentRequestRepository(accepted, missingTransfer, pending, foreign);
var wallets = new InMemoryWalletRepository([
    Wallet.Create(requesterWallet, ownerId, currency, null, now),
    Wallet.Create(foreignRequesterWallet, foreignOwnerId, currency, null, now),
    Wallet.Create(payerWallet, payerOwnerId, currency, null, now)
]);

var transferId = Guid.NewGuid();
var receipts = new FixedReceiptReader(new Dictionary<Guid, PaymentRequestReconciliationReceipt>
{
    [accepted.Id.Value] = new(
        transferId,
        payerWallet.Value,
        requesterWallet.Value,
        currency.Code,
        accepted.AmountMinor,
        accepted.Id.Value,
        now.AddMinutes(2))
});

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services
    .AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPaymentRequestRepository>(repository);
builder.Services.AddSingleton<IWalletRepository>(wallets);
builder.Services.AddSingleton<IPaymentRequestPaymentReceiptReader>(receipts);
builder.Services.AddSingleton<PaymentRequestReconciliationService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapPaymentRequestReconciliationEndpoints();
await app.StartAsync();

Assert(app.Services.GetService<IPaymentRequestPaymentPort>() is null,
    "Reconciliation HTTP harness must not contain a payment execution port.");

var anonymous = app.GetTestClient();
var owner = CreateClient(app, ownerId);

await RunAsync("reconciliation requires authentication", async () =>
{
    var response = await anonymous.PostAsync($"/api/v1/payment-requests/{accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("foreign requester is hidden", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{foreign.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("pending request is not eligible", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{pending.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    Assert(pending.Status == PaymentRequestStatus.Pending, "Pending request must remain unchanged.");
});

await RunAsync("missing certified transfer is conflict without mutation", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{missingTransfer.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    Assert(missingTransfer.Status == PaymentRequestStatus.Accepted, "Accepted request must remain Accepted when no transfer exists.");
});

await RunAsync("matching certified transfer repairs accepted request to paid", async () =>
{
    var response = await owner.PostAsync($"/api/v1/payment-requests/{accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PaymentRequestReconciliationHttpResponse>();
    Assert(body?.ReconciliationStatus == nameof(PaymentRequestReconciliationStatus.Reconciled), "Expected Reconciled response.");
    Assert(body?.Status == nameof(PaymentRequestStatus.Paid), "Expected Paid status.");
    Assert(body?.TransferId == transferId, "Transfer id must come from certified receipt.");
    Assert(accepted.Status == PaymentRequestStatus.Paid, "Stored request must become Paid.");
});

await RunAsync("reconciliation replay is idempotent", async () =>
{
    var updatesBefore = repository.UpdateCalls;
    var response = await owner.PostAsync($"/api/v1/payment-requests/{accepted.Id.Value}/reconcile", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PaymentRequestReconciliationHttpResponse>();
    Assert(body?.ReconciliationStatus == nameof(PaymentRequestReconciliationStatus.AlreadyPaid), "Expected AlreadyPaid replay.");
    Assert(repository.UpdateCalls == updatesBefore, "AlreadyPaid replay must not write again.");
});

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-1 protected trigger scenarios: PASS");
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

sealed class FixedReceiptReader(IReadOnlyDictionary<Guid, PaymentRequestReconciliationReceipt> receipts)
    : IPaymentRequestPaymentReceiptReader
{
    public Task<PaymentRequestReconciliationReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(receipts.TryGetValue(correlationId, out var receipt)
            ? receipt
            : null);
    }
}

sealed class InMemoryPaymentRequestRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = requests.ToDictionary(x => x.Id.Value, x => x);
    private readonly Dictionary<Guid, PaymentRequest> correlations = requests.ToDictionary(x => x.CorrelationId, x => x);
    public int UpdateCalls { get; private set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        correlations.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        correlations[request.CorrelationId] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[request.Id.Value] = request;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(x => x.Id.Value, x => x);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(walletId.Value, out var wallet) ? wallet : null);

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        values[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
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
