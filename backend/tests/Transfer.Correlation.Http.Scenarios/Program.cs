using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.Transfer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var sourceOwnerId = Guid.NewGuid();
var targetOwnerId = Guid.NewGuid();
var outsiderId = Guid.NewGuid();
var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var transferId = TransferId.New();
var journalEntryId = JournalEntryId.New();
var postedAt = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);

var receipt = TransferReceiptReadModel.Create(
    transferId,
    journalEntryId,
    sourceWalletId,
    targetWalletId,
    "XAF",
    9_500,
    correlationId,
    postedAt);

var walletRepository = new FakeWalletRepository([
    Wallet.Create(WalletId.From(sourceWalletId), sourceOwnerId, Currency.Create("XAF"), null, postedAt),
    Wallet.Create(WalletId.From(targetWalletId), targetOwnerId, Currency.Create("XAF"), null, postedAt)
]);
var receiptReader = new FakeReceiptReader(receipt);

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services
    .AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IWalletRepository>(walletRepository);
builder.Services.AddSingleton<ITransferReceiptReader>(receiptReader);
builder.Services.AddScoped<TransferCorrelationLookupService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapTransferEndpoints();
await app.StartAsync();

var anonymous = app.GetTestClient();
var sourceOwner = CreateClient(app, sourceOwnerId);
var targetOwner = CreateClient(app, targetOwnerId);
var outsider = CreateClient(app, outsiderId);

await RunAsync("receipt lookup requires authentication", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/transfers/by-correlation/{correlationId}/receipt");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("source owner can read receipt", async () =>
{
    var response = await sourceOwner.GetAsync($"/api/v1/transfers/by-correlation/{correlationId}/receipt");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<TransferReceiptResponse>();
    Assert(body is not null, "Receipt response is required.");
    Assert(body!.TransferId == transferId.Value, "Transfer id mismatch.");
    Assert(body.SourceWalletId == sourceWalletId, "Source wallet mismatch.");
    Assert(body.TargetWalletId == targetWalletId, "Target wallet mismatch.");
    Assert(body.CurrencyCode == "XAF", "Currency mismatch.");
    Assert(body.AmountMinor == 9_500, "Amount mismatch.");
    Assert(body.CorrelationId == correlationId, "Correlation mismatch.");
    Assert(body.JournalEntryId == journalEntryId.Value, "Journal id mismatch.");
    Assert(body.PostedAtUtc == postedAt, "Posting timestamp mismatch.");
});

await RunAsync("target owner can read receipt", async () =>
{
    var response = await targetOwner.GetAsync($"/api/v1/transfers/by-correlation/{correlationId}/receipt");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
});

await RunAsync("unrelated user cannot enumerate receipt", async () =>
{
    var response = await outsider.GetAsync($"/api/v1/transfers/by-correlation/{correlationId}/receipt");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.NotFound, "Expected TRANSFER_NOT_FOUND.");
});

await RunAsync("missing correlation returns 404", async () =>
{
    var response = await sourceOwner.GetAsync($"/api/v1/transfers/by-correlation/{Guid.NewGuid()}/receipt");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("empty correlation returns 400", async () =>
{
    var response = await sourceOwner.GetAsync($"/api/v1/transfers/by-correlation/{Guid.Empty}/receipt");
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.ValidationError, "Expected TRANSFER_VALIDATION_ERROR.");
});

Assert(receiptReader.LookupCalls == 4, "Receipt reader must only run for authenticated non-empty lookups.");
Console.WriteLine("AFW-BE-TRANSFER-CORRELATION-1 protected receipt HTTP scenarios: PASS");
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

sealed class FakeReceiptReader(TransferReceiptReadModel receipt) : ITransferReceiptReader
{
    public int LookupCalls { get; private set; }

    public Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LookupCalls++;
        return Task.FromResult<TransferReceiptReadModel?>(
            receipt.CorrelationId == correlationId ? receipt : null);
    }
}

sealed class FakeWalletRepository(IEnumerable<Wallet> wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> values = wallets.ToDictionary(wallet => wallet.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Any(wallet =>
            wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read-only HTTP scenarios must not add wallets.");

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.GetValueOrDefault(walletId.Value));
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(values.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read-only HTTP scenarios must not update wallets.");
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
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
