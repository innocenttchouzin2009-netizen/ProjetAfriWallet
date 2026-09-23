using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using IdentityService.Api.MerchantPayouts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await ReconciliationFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var owner = fixture.CreateClient("merchant.alpha");
var foreign = fixture.CreateClient("merchant.beta");
var authenticatedWithoutMerchant = fixture.CreateClient(null);

await RunAsync("authentication required", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/latest");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Expected 401.");
});

await RunAsync("merchant claim required", async () =>
{
    var response = await authenticatedWithoutMerchant.GetAsync($"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/latest");
    Assert(response.StatusCode == HttpStatusCode.Forbidden, "Expected 403.");
});

await RunAsync("foreign merchant payout is hidden", async () =>
{
    var response = await foreign.GetAsync($"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/latest");
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Expected 404.");
});

var matchedResultId = Guid.NewGuid();
await RunAsync("matching provider result reconciles as matched", async () =>
{
    var request = new ReconcileMerchantPayoutProviderResultRequest(
        matchedResultId,
        MerchantPayoutProviderResultStatus.Succeeded,
        "provider-ref-ok",
        null,
        250_000,
        "XAF",
        fixture.ObservedAtUtc);

    var response = await owner.PostAsJsonAsync(
        $"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/provider-results",
        request);

    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var result = await response.Content.ReadFromJsonAsync<MerchantPayoutReconciliationResponse>();
    Assert(result is not null, "Response required.");
    Assert(result.Status == "Matched", "Expected Matched.");
    Assert(result.ReasonCode == MerchantPayoutReconciliationReasonCode.Matched, "Expected matched reason.");
});

await RunAsync("same provider result is idempotent", async () =>
{
    var request = new ReconcileMerchantPayoutProviderResultRequest(
        matchedResultId,
        MerchantPayoutProviderResultStatus.Succeeded,
        "provider-ref-ok",
        null,
        250_000,
        "XAF",
        fixture.ObservedAtUtc);

    var first = await owner.PostAsJsonAsync(
        $"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/provider-results",
        request);
    var second = await owner.PostAsJsonAsync(
        $"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/provider-results",
        request);

    var a = await first.Content.ReadFromJsonAsync<MerchantPayoutReconciliationResponse>();
    var b = await second.Content.ReadFromJsonAsync<MerchantPayoutReconciliationResponse>();
    Assert(first.StatusCode == HttpStatusCode.OK && second.StatusCode == HttpStatusCode.OK, "Expected 200.");
    Assert(a?.ReconciliationId == b?.ReconciliationId, "Idempotent result must reuse reconciliation.");
});

await RunAsync("amount mismatch is classified deterministically", async () =>
{
    var request = new ReconcileMerchantPayoutProviderResultRequest(
        Guid.NewGuid(),
        MerchantPayoutProviderResultStatus.Succeeded,
        "provider-ref-mismatch",
        null,
        249_999,
        "XAF",
        fixture.ObservedAtUtc);

    var response = await owner.PostAsJsonAsync(
        $"/api/v1/merchant-payouts/{fixture.MismatchPayoutId:D}/reconciliation/provider-results",
        request);

    var result = await response.Content.ReadFromJsonAsync<MerchantPayoutReconciliationResponse>();
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    Assert(result?.Status == "Mismatch", "Expected Mismatch.");
    Assert(result?.ReasonCode == MerchantPayoutReconciliationReasonCode.AmountMismatch, "Expected amount mismatch.");
});

await RunAsync("latest reconciliation can be read by owner", async () =>
{
    var response = await owner.GetAsync(
        $"/api/v1/merchant-payouts/{fixture.MatchedPayoutId:D}/reconciliation/latest");
    var result = await response.Content.ReadFromJsonAsync<MerchantPayoutReconciliationResponse>();
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    Assert(result?.ResultId == matchedResultId, "Latest reconciliation result id mismatch.");
});

Console.WriteLine("AFW-BE-MERCHANT-PAYOUT-RECONCILIATION-1 protected HTTP scenarios: PASS");

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
        MerchantPayoutExecution matched,
        MerchantPayoutExecution mismatch,
        DateTimeOffset observedAtUtc)
    {
        App = app;
        MatchedPayoutId = matched.PayoutId;
        MismatchPayoutId = mismatch.PayoutId;
        ObservedAtUtc = observedAtUtc;
    }

    public WebApplication App { get; }
    public Guid MatchedPayoutId { get; }
    public Guid MismatchPayoutId { get; }
    public DateTimeOffset ObservedAtUtc { get; }

    public static async Task<ReconciliationFixture> CreateAsync()
    {
        var now = new DateTimeOffset(2026, 9, 23, 18, 30, 0, TimeSpan.Zero);
        var observed = now.AddMinutes(-1);
        var matched = MerchantPayoutExecution.Restore(
            Guid.NewGuid(), Guid.NewGuid(), "merchant.alpha", 250_000, "XAF", Guid.NewGuid(),
            "idem-match", MerchantPayoutStatus.Succeeded, "provider-ref-ok", null, now.AddMinutes(-10), now.AddMinutes(-5));
        var mismatch = MerchantPayoutExecution.Restore(
            Guid.NewGuid(), Guid.NewGuid(), "merchant.alpha", 250_000, "XAF", Guid.NewGuid(),
            "idem-mismatch", MerchantPayoutStatus.Succeeded, "provider-ref-mismatch", null, now.AddMinutes(-10), now.AddMinutes(-5));

        var payouts = new InMemoryPayoutRepository(matched, mismatch);
        var providerResults = new InMemoryProviderResultStore();
        var reconciliations = new InMemoryReconciliationStore();
        var service = new MerchantPayoutReconciliationService(
            payouts, providerResults, reconciliations, new MerchantPayoutReconciliationPolicy(),
            new FixedTimeProvider(now));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IMerchantPayoutRepository>(payouts);
        builder.Services.AddSingleton(service);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapMerchantPayoutReconciliationEndpoints();
        await app.StartAsync();

        return new ReconciliationFixture(app, matched, mismatch, observed);
    }

    public HttpClient CreateClient(string? merchantId)
    {
        var client = App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        if (!string.IsNullOrWhiteSpace(merchantId))
            client.DefaultRequestHeaders.Add("X-Test-Merchant", merchantId);
        return client;
    }

    public ValueTask DisposeAsync() => App.DisposeAsync();
}

sealed class InMemoryPayoutRepository(params MerchantPayoutExecution[] seeded) : IMerchantPayoutRepository
{
    private readonly Dictionary<Guid, MerchantPayoutExecution> values = seeded.ToDictionary(x => x.PayoutId);

    public Task AddAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default)
    {
        values.Add(payout.PayoutId, payout);
        return Task.CompletedTask;
    }

    public Task SaveAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default)
    {
        values[payout.PayoutId] = payout;
        return Task.CompletedTask;
    }

    public Task<MerchantPayoutExecution?> GetAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        values.TryGetValue(payoutId, out var value);
        return Task.FromResult(value);
    }

    public Task<MerchantPayoutExecution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

    public Task<MerchantPayoutExecution?> GetByReceivableAsync(Guid receivableId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.FirstOrDefault(x => x.ReceivableId == receivableId));
}

sealed class InMemoryProviderResultStore : IMerchantPayoutProviderResultStore
{
    private readonly Dictionary<Guid, MerchantPayoutProviderResultRecord> values = [];

    public Task SaveAsync(MerchantPayoutProviderResultRecord result, CancellationToken cancellationToken = default)
    {
        if (values.TryGetValue(result.ResultId, out var existing) && existing != result)
            throw new InvalidOperationException("Provider result is immutable.");
        values[result.ResultId] = result;
        return Task.CompletedTask;
    }

    public Task<MerchantPayoutProviderResultRecord?> GetAsync(Guid resultId, CancellationToken cancellationToken = default)
    {
        values.TryGetValue(resultId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<MerchantPayoutProviderResultRecord>> ListForPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MerchantPayoutProviderResultRecord>>(
            values.Values.Where(x => x.PayoutId == payoutId).OrderBy(x => x.ObservedAtUtc).ToArray());
}

sealed class InMemoryReconciliationStore : IMerchantPayoutReconciliationStore
{
    private readonly Dictionary<Guid, MerchantPayoutReconciliationRecord> values = [];

    public Task SaveAsync(MerchantPayoutReconciliationRecord reconciliation, CancellationToken cancellationToken = default)
    {
        values[reconciliation.ReconciliationId] = reconciliation;
        return Task.CompletedTask;
    }

    public Task<MerchantPayoutReconciliationRecord?> GetAsync(Guid reconciliationId, CancellationToken cancellationToken = default)
    {
        values.TryGetValue(reconciliationId, out var value);
        return Task.FromResult(value);
    }

    public Task<MerchantPayoutReconciliationRecord?> GetLatestForPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.Where(x => x.PayoutId == payoutId)
            .OrderByDescending(x => x.EvaluatedAtUtc).ThenByDescending(x => x.ReconciliationId).FirstOrDefault());

    public Task<MerchantPayoutReconciliationRecord?> GetForResultAsync(Guid resultId, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.Values.SingleOrDefault(x => x.ResultId == resultId));
}

sealed class HeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var rawUser) ||
            !Guid.TryParse(rawUser.ToString(), out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", userId.ToString()) };
        if (Request.Headers.TryGetValue("X-Test-Merchant", out var merchant))
            claims.Add(new Claim("merchant_id", merchant.ToString()));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
