using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var actorId = Guid.NewGuid();
var foreignActorId = Guid.NewGuid();
var eventId = Guid.NewGuid();

await using var fixture = await RecoveryHttpFixture.CreateAsync(actorId, eventId);
var anonymous = fixture.App.GetTestClient();
var actorClient = CreateClient(fixture.App, actorId);
var foreignClient = CreateClient(fixture.App, foreignActorId);

await RunAsync("anonymous recovery is rejected", async () =>
{
    var response = await anonymous.PostAsJsonAsync(
        $"/api/v1/ops/payment-request-event-outbox/dead-letter/{eventId}/recover",
        new PaymentRequestDeadLetterRecoveryHttpRequest("manual replay", null));
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("non-ops authenticated actor is forbidden", async () =>
{
    var response = await foreignClient.PostAsJsonAsync(
        $"/api/v1/ops/payment-request-event-outbox/dead-letter/{eventId}/recover",
        new PaymentRequestDeadLetterRecoveryHttpRequest("manual replay", null));
    Assert(response.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)response.StatusCode}.");
    Assert(fixture.Store.RequeueCalls == 0, "Forbidden actor must not reach recovery store mutation.");
});

await RunAsync("blank reason is rejected before recovery", async () =>
{
    var response = await actorClient.PostAsJsonAsync(
        $"/api/v1/ops/payment-request-event-outbox/dead-letter/{eventId}/recover",
        new PaymentRequestDeadLetterRecoveryHttpRequest("   ", null));
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    Assert(fixture.Store.RequeueCalls == 0, "Invalid input must not requeue.");
});

await RunAsync("authorized ops actor requeues through application service", async () =>
{
    var response = await actorClient.PostAsJsonAsync(
        $"/api/v1/ops/payment-request-event-outbox/dead-letter/{eventId}/recover",
        new PaymentRequestDeadLetterRecoveryHttpRequest("provider incident resolved", null));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PaymentRequestDeadLetterRecoveryHttpResponse>();
    Assert(body?.Status == nameof(PaymentRequestEventDeadLetterRecoveryExecutionCode.Requeued), "Expected Requeued status.");
    Assert(body?.ReplayOrdinal == 1, "First recovery must have replay ordinal 1.");
    Assert(fixture.Store.RequeueCalls == 1, "Recovery store must be mutated exactly once.");
    Assert(fixture.Store.LastPlan?.RequestedBy == actorId.ToString("D"), "RequestedBy must derive from authenticated sub.");
});

await RunAsync("unknown dead-letter event returns 404", async () =>
{
    var response = await actorClient.PostAsJsonAsync(
        $"/api/v1/ops/payment-request-event-outbox/dead-letter/{Guid.NewGuid()}/recover",
        new PaymentRequestDeadLetterRecoveryHttpRequest("manual replay", null));
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Assert(fixture.App.Services.GetService<IPaymentRequestEventDeliveryPort>() is null,
    "HTTP recovery composition must not require or resolve the event dispatcher/delivery port.");

Console.WriteLine("Protected dead-letter recovery HTTP scenarios passed.");

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

sealed class RecoveryHttpFixture : IAsyncDisposable
{
    private RecoveryHttpFixture(WebApplication app, RecordingRecoveryStore store)
    {
        App = app;
        Store = store;
    }

    public WebApplication App { get; }
    public RecordingRecoveryStore Store { get; }

    public static async Task<RecoveryHttpFixture> CreateAsync(Guid actorId, Guid eventId)
    {
        var now = new DateTimeOffset(2026, 9, 18, 18, 0, 0, TimeSpan.Zero);
        var requestId = PaymentRequestId.From(Guid.NewGuid());
        var item = new PaymentRequestEventOutboxItem(
            new PaymentRequestEventEnvelope(eventId, requestId, "PaymentRequestCreated", now.AddMinutes(-10), "{}"),
            PaymentRequestEventOutboxStatus.DeadLetter,
            1,
            now.AddMinutes(-10),
            now.AddMinutes(-10),
            null,
            now.AddMinutes(-1),
            null,
            "provider unavailable");

        var attempt = new PaymentRequestEventAttempt(
            Guid.NewGuid(),
            eventId,
            1,
            now.AddMinutes(-2),
            now.AddMinutes(-1),
            PaymentRequestEventAttemptOutcome.DeadLetter,
            "provider unavailable",
            null);

        var store = new RecordingRecoveryStore(item);
        var ledger = new FixedAttemptLedger(attempt);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(new PaymentRequestDeadLetterRecoveryHttpOptions([actorId]));
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        builder.Services.AddSingleton<IPaymentRequestEventDeadLetterRecoveryStore>(store);
        builder.Services.AddSingleton<IPaymentRequestEventAttemptLedger>(ledger);
        builder.Services.AddSingleton(new PaymentRequestEventDeadLetterReplayPolicy());
        builder.Services.AddScoped<PaymentRequestEventDeadLetterRecoveryService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestDeadLetterRecoveryEndpoints();
        await app.StartAsync();

        return new RecoveryHttpFixture(app, store);
    }

    public ValueTask DisposeAsync() => App.DisposeAsync();
}

sealed class RecordingRecoveryStore(PaymentRequestEventOutboxItem item) : IPaymentRequestEventDeadLetterRecoveryStore
{
    public int RequeueCalls { get; private set; }
    public PaymentRequestEventDeadLetterReplayPlan? LastPlan { get; private set; }

    public Task<PaymentRequestEventOutboxItem?> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(eventId == item.Event.EventId ? item : null);
    }

    public Task<bool> TryRequeueAsync(PaymentRequestEventDeadLetterReplayPlan plan, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequeueCalls++;
        LastPlan = plan;
        return Task.FromResult(true);
    }
}

sealed class FixedAttemptLedger(PaymentRequestEventAttempt attempt) : IPaymentRequestEventAttemptLedger
{
    public Task<Guid> BeginAttemptAsync(Guid eventId, int attemptNumber, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task CompleteAttemptAsync(Guid attemptId, PaymentRequestEventAttemptOutcome outcome, DateTimeOffset completedAtUtc, string? error = null, DateTimeOffset? nextAttemptAtUtc = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<PaymentRequestEventAttempt>> ListByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PaymentRequestEventAttempt> result =
            eventId == attempt.EventId ? [attempt] : Array.Empty<PaymentRequestEventAttempt>();
        return Task.FromResult(result);
    }
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
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
