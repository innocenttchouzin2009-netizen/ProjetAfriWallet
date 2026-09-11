using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await Fixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var requesterClient = Client(fixture.App, fixture.RequesterOwnerId);
var payerClient = Client(fixture.App, fixture.PayerOwnerId);
var strangerClient = Client(fixture.App, Guid.NewGuid());

var authRequest = fixture.SeedRequest();
var anonymousResponse = await anonymous.PostAsync($"/api/v1/payment-requests/{authRequest.Id.Value}/accept", null);
Assert(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "Accept action must require authentication.");

var acceptRequest = fixture.SeedRequest();
var wrongAccept = await requesterClient.PostAsync($"/api/v1/payment-requests/{acceptRequest.Id.Value}/accept", null);
Assert(wrongAccept.StatusCode == HttpStatusCode.NotFound, "Requester must not impersonate payer on accept.");
Assert(fixture.TransferPort.Calls == 0, "Unauthorized accept must not move funds.");

var accepted = await payerClient.PostAsync($"/api/v1/payment-requests/{acceptRequest.Id.Value}/accept", null);
Assert(accepted.StatusCode == HttpStatusCode.OK, $"Expected 200 for payer accept, got {(int)accepted.StatusCode}.");
var paid = await accepted.Content.ReadFromJsonAsync<PaymentRequestHttpResponse>();
Assert(paid?.Status == "Paid", "Successful accept must return Paid.");
Assert(paid?.TransferId == fixture.TransferPort.LastTransferId, "Paid response must contain actual transfer id.");
Assert(fixture.TransferPort.Calls == 1, "Accept must execute one transfer.");
Assert(fixture.TransferPort.SourceWalletId == fixture.PayerWalletId, "Transfer source must be payer wallet.");
Assert(fixture.TransferPort.TargetWalletId == fixture.RequesterWalletId, "Transfer target must be requester wallet.");
Assert(fixture.TransferPort.CorrelationId == acceptRequest.Id.Value, "Transfer correlation must be payment request id.");

var cancelRequest = fixture.SeedRequest();
var wrongCancel = await payerClient.PostAsync($"/api/v1/payment-requests/{cancelRequest.Id.Value}/cancel", null);
Assert(wrongCancel.StatusCode == HttpStatusCode.NotFound, "Payer must not cancel requester-owned request.");
var cancelled = await requesterClient.PostAsync($"/api/v1/payment-requests/{cancelRequest.Id.Value}/cancel", null);
Assert(cancelled.StatusCode == HttpStatusCode.OK, "Requester must be able to cancel.");
var cancelledBody = await cancelled.Content.ReadFromJsonAsync<PaymentRequestHttpResponse>();
Assert(cancelledBody?.Status == "Cancelled", "Cancel must return Cancelled.");

var declineRequest = fixture.SeedRequest();
var wrongDecline = await strangerClient.PostAsync($"/api/v1/payment-requests/{declineRequest.Id.Value}/decline", null);
Assert(wrongDecline.StatusCode == HttpStatusCode.NotFound, "Foreign actor must not decline.");
var declined = await payerClient.PostAsync($"/api/v1/payment-requests/{declineRequest.Id.Value}/decline", null);
Assert(declined.StatusCode == HttpStatusCode.OK, "Payer must be able to decline.");
var declinedBody = await declined.Content.ReadFromJsonAsync<PaymentRequestHttpResponse>();
Assert(declinedBody?.Status == "Declined", "Decline must return Declined.");

Console.WriteLine("AFW-BE-REQUEST-1 authenticated action HTTP scenarios: PASS");

static HttpClient Client(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class Fixture : IAsyncDisposable
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);

    private Fixture(
        WebApplication app,
        InMemoryRequestRepository repository,
        RecordingP2PTransferPort transferPort,
        Guid requesterOwnerId,
        Guid payerOwnerId,
        Guid requesterWalletId,
        Guid payerWalletId)
    {
        App = app;
        Repository = repository;
        TransferPort = transferPort;
        RequesterOwnerId = requesterOwnerId;
        PayerOwnerId = payerOwnerId;
        RequesterWalletId = requesterWalletId;
        PayerWalletId = payerWalletId;
    }

    public WebApplication App { get; }
    public InMemoryRequestRepository Repository { get; }
    public RecordingP2PTransferPort TransferPort { get; }
    public Guid RequesterOwnerId { get; }
    public Guid PayerOwnerId { get; }
    public Guid RequesterWalletId { get; }
    public Guid PayerWalletId { get; }

    public PaymentRequest SeedRequest()
    {
        var request = PaymentRequest.Create(
            WalletId.From(RequesterWalletId),
            RecipientReference.FromAfWalId("payer.one"),
            Currency.Create("XAF"),
            2_500,
            Guid.NewGuid(),
            CreatedAtUtc,
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Repository.Seed(request);
        return request;
    }

    public static async Task<Fixture> CreateAsync()
    {
        var requesterOwnerId = Guid.NewGuid();
        var payerOwnerId = Guid.NewGuid();
        var requesterWalletId = Guid.NewGuid();
        var payerWalletId = Guid.NewGuid();
        var wallets = new InMemoryWalletRepository([
            Wallet.Create(WalletId.From(requesterWalletId), requesterOwnerId, Currency.Create("XAF"), null, CreatedAtUtc),
            Wallet.Create(WalletId.From(payerWalletId), payerOwnerId, Currency.Create("XAF"), null, CreatedAtUtc)
        ]);
        var repository = new InMemoryRequestRepository();
        var resolver = new FixedResolver(WalletId.From(payerWalletId));
        var transferPort = new RecordingP2PTransferPort();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IWalletRepository>(wallets);
        builder.Services.AddSingleton<IPaymentRequestRepository>(repository);
        builder.Services.AddSingleton<IPaymentRequestRecipientResolver>(resolver);
        builder.Services.AddSingleton<IPaymentRequestWalletOwnershipReader, WalletPaymentRequestOwnershipReader>();
        builder.Services.AddSingleton<IP2PTransferPort>(transferPort);
        builder.Services.AddSingleton<IPaymentRequestPaymentPort, P2PPaymentRequestPaymentPort>();
        builder.Services.AddSingleton<PaymentRequestApplicationService>();
        builder.Services.AddSingleton<PaymentRequestActionService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestEndpoints();
        await app.StartAsync();

        return new Fixture(app, repository, transferPort, requesterOwnerId, payerOwnerId, requesterWalletId, payerWalletId);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryRequestRepository : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> values = new();
    private readonly Dictionary<Guid, Guid> correlations = new();

    public void Seed(PaymentRequest request)
    {
        values[request.Id.Value] = request;
        correlations[request.CorrelationId] = request.Id.Value;
    }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(id.Value, out var value) ? value : null);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(correlations.TryGetValue(correlationId, out var id) && values.TryGetValue(id, out var value) ? value : null);
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Seed(request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Seed(request);
        return Task.CompletedTask;
    }
}

sealed class FixedResolver(WalletId walletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<WalletId?>(walletId);
    }
}

sealed class RecordingP2PTransferPort : IP2PTransferPort
{
    public int Calls { get; private set; }
    public Guid LastTransferId { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid TargetWalletId { get; private set; }
    public Guid CorrelationId { get; private set; }

    public Task<P2PTransferReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastTransferId = Guid.NewGuid();
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        CorrelationId = correlationId;
        return Task.FromResult(new P2PTransferReceipt(
            LastTransferId,
            sourceWalletId,
            targetWalletId,
            "XAF",
            amountMinor,
            correlationId,
            requestedAtUtc));
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
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
