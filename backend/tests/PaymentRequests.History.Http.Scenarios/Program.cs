using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var composition = new ServiceCollection();
composition.AddPaymentRequests("Data Source=:memory:");
Assert(composition.Any(d => d.ServiceType == typeof(IPaymentRequestHistoryReader) && d.ImplementationType == typeof(EfPaymentRequestHistoryReader)),
    "Composition must register IPaymentRequestHistoryReader -> EfPaymentRequestHistoryReader.");
Assert(composition.Any(d => d.ServiceType == typeof(PaymentRequestHistoryService)),
    "Composition must register PaymentRequestHistoryService.");

var userId = Guid.NewGuid();
var ownedWallet = WalletId.From(Guid.NewGuid());
var ownedReference = RecipientReference.FromAfWalId("owner.afwal");
var historyReader = new RecordingHistoryReader();

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPaymentRequestOwnedWalletReader>(new FixedOwnedWalletReader(userId, [ownedWallet]));
builder.Services.AddSingleton<IPaymentRequestOwnedRecipientReferenceReader>(new FixedOwnedReferenceReader(userId, [ownedReference]));
builder.Services.AddSingleton<IPaymentRequestHistoryReader>(historyReader);
builder.Services.AddScoped<PaymentRequestHistoryService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapPaymentRequestHistoryEndpoints();
await app.StartAsync();

var anonymous = app.GetTestClient();
var anonymousResponse = await anonymous.GetAsync("/api/v1/payment-requests/history");
Assert(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "History must require authentication.");

var client = app.GetTestClient();
client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
var response = await client.GetAsync("/api/v1/payment-requests/history?direction=received&status=Pending,Paid&page=1&pageSize=10&order=oldest&walletId=00000000-0000-0000-0000-000000000001&afWalId=attacker.afwal");
Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
Assert(historyReader.LastQuery is not null, "History reader must be called.");
Assert(historyReader.LastQuery!.Direction == PaymentRequestHistoryDirection.Received, "Direction filter mismatch.");
Assert(historyReader.LastQuery.Page.PageNumber == 1 && historyReader.LastQuery.Page.PageSize == 10, "Paging mismatch.");
Assert(historyReader.LastQuery.Order == PaymentRequestTemporalOrder.OldestFirst, "Order mismatch.");
Assert(historyReader.LastQuery.Statuses is not null && historyReader.LastQuery.Statuses.Contains(PaymentRequestStatus.Pending) && historyReader.LastQuery.Statuses.Contains(PaymentRequestStatus.Paid), "Status filter mismatch.");
Assert(historyReader.LastQuery.OwnedWalletIds.SequenceEqual([ownedWallet]), "Wallet scope must be server-derived.");
Assert(historyReader.LastQuery.OwnedRecipientReferences.SequenceEqual([ownedReference]), "Recipient scope must be server-derived.");

var payload = await response.Content.ReadFromJsonAsync<PaymentRequestHistoryPageResponse>();
Assert(payload is not null && payload.Items.Count == 1, "History response expected.");
var raw = await response.Content.ReadAsStringAsync();
Assert(!raw.Contains("owner.afwal", StringComparison.Ordinal), "Raw AfWal ID must not be exposed in history response.");
Assert(!raw.Contains("attacker.afwal", StringComparison.Ordinal), "Client-supplied AfWal ID must not influence or leak into response.");

foreach (var invalid in new[]
{
    "/api/v1/payment-requests/history?direction=other",
    "/api/v1/payment-requests/history?status=Unknown",
    "/api/v1/payment-requests/history?page=-1",
    "/api/v1/payment-requests/history?order=random"
})
{
    var invalidResponse = await client.GetAsync(invalid);
    Assert(invalidResponse.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 for {invalid}.");
}

Console.WriteLine("AFW-BE-REQUEST-HISTORY-1 protected HTTP composition scenarios: PASS");
await app.DisposeAsync();

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class RecordingHistoryReader : IPaymentRequestHistoryReader
{
    public PaymentRequestHistoryReadQuery? LastQuery { get; private set; }

    public Task<PaymentRequestHistoryPage> ListAsync(PaymentRequestHistoryReadQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastQuery = query;
        var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
        var item = new PaymentRequestQueryItem(
            PaymentRequestId.From(Guid.NewGuid()),
            query.OwnedWalletIds.First(),
            RecipientReferenceKind.AfWalId,
            Currency.Create("EUR"),
            2500,
            Guid.NewGuid(),
            now,
            null,
            now,
            PaymentRequestStatus.Pending,
            null,
            null,
            null,
            null);
        return Task.FromResult(new PaymentRequestHistoryPage(
            [new PaymentRequestHistoryItem(item, PaymentRequestHistoryDirection.Received)],
            query.Page.PageNumber,
            query.Page.PageSize,
            1,
            false));
    }
}

sealed class FixedOwnedWalletReader(Guid expectedUserId, IReadOnlyCollection<WalletId> wallets) : IPaymentRequestOwnedWalletReader
{
    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId != expectedUserId) throw new InvalidOperationException("History scope must use authenticated user id.");
        return Task.FromResult(wallets);
    }
}

sealed class FixedOwnedReferenceReader(Guid expectedUserId, IReadOnlyCollection<RecipientReference> references) : IPaymentRequestOwnedRecipientReferenceReader
{
    public Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId != expectedUserId) throw new InvalidOperationException("History scope must use authenticated user id.");
        return Task.FromResult(references);
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
