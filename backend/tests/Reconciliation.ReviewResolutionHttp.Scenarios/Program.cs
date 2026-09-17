using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reconciliation.Api.Review;
using Reconciliation.Application.Review;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.ReviewResolution;

var reviewId = Guid.NewGuid();
var resolverId = Guid.NewGuid().ToString();
var queuedAt = DateTime.UtcNow.AddMinutes(-5);
var review = new ReconciliationReviewItem(
    reviewId,
    "partner-1",
    "internal-1",
    "external-1",
    ReconciliationMatchType.Exact,
    100,
    0,
    TimeSpan.Zero,
    queuedAt,
    ReconciliationReviewStatus.Approved,
    "reviewer-1",
    null,
    queuedAt.AddMinutes(1));
var evidence = new ReviewResolutionEvidence(
    $"reconciliation:{reviewId:N}",
    "internal-1",
    "external-1",
    queuedAt.AddMinutes(1));
var store = new InMemoryResolutionStore();

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IReviewResolutionReviewReader>(new FixedReviewReader(review));
builder.Services.AddSingleton<IReviewResolutionEvidenceMatcher>(new FixedEvidenceMatcher(evidence));
builder.Services.AddSingleton<IReviewResolutionStore>(store);
builder.Services.AddScoped<ReviewResolutionApplicationService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapReviewResolutionEndpoints();
await app.StartAsync();

var anonymous = app.GetTestClient();
var unauthenticated = await anonymous.PostAsync($"/api/v1/reconciliation/reviews/{reviewId}/resolve", null);
Assert(unauthenticated.StatusCode == HttpStatusCode.Unauthorized, "Resolve endpoint must require authentication.");

var client = app.GetTestClient();
client.DefaultRequestHeaders.Add("X-Test-User", resolverId);
var first = await client.PostAsync($"/api/v1/reconciliation/reviews/{reviewId}/resolve", null);
Assert(first.StatusCode == HttpStatusCode.OK, "Approved review must resolve successfully.");
var firstBody = await first.Content.ReadFromJsonAsync<ReviewResolutionResponse>();
Assert(firstBody?.Status == nameof(ReviewResolutionStatus.Resolved), "First resolution must report Resolved.");
Assert(firstBody?.ResolvedBy == resolverId, "Resolver id must come from authenticated subject.");
Assert(store.Count == 1, "Resolution must be durably stored once.");

var replay = await client.PostAsync($"/api/v1/reconciliation/reviews/{reviewId}/resolve", null);
Assert(replay.StatusCode == HttpStatusCode.OK, "Idempotent replay must succeed.");
var replayBody = await replay.Content.ReadFromJsonAsync<ReviewResolutionResponse>();
Assert(replayBody?.Status == nameof(ReviewResolutionStatus.AlreadyResolved), "Replay must report AlreadyResolved.");
Assert(store.Count == 1, "Idempotent replay must not create a second resolution.");

var notEligibleBuilder = WebApplication.CreateBuilder();
notEligibleBuilder.WebHost.UseTestServer();
notEligibleBuilder.Services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
notEligibleBuilder.Services.AddAuthorization();
notEligibleBuilder.Services.AddSingleton<IReviewResolutionReviewReader>(new FixedReviewReader(review with { Status = ReconciliationReviewStatus.Rejected }));
notEligibleBuilder.Services.AddSingleton<IReviewResolutionEvidenceMatcher>(new FixedEvidenceMatcher(evidence));
notEligibleBuilder.Services.AddSingleton<IReviewResolutionStore>(new InMemoryResolutionStore());
notEligibleBuilder.Services.AddScoped<ReviewResolutionApplicationService>();
var notEligibleApp = notEligibleBuilder.Build();
notEligibleApp.UseAuthentication();
notEligibleApp.UseAuthorization();
notEligibleApp.MapReviewResolutionEndpoints();
await notEligibleApp.StartAsync();
var notEligibleClient = notEligibleApp.GetTestClient();
notEligibleClient.DefaultRequestHeaders.Add("X-Test-User", resolverId);
var notEligible = await notEligibleClient.PostAsync($"/api/v1/reconciliation/reviews/{reviewId}/resolve", null);
Assert(notEligible.StatusCode == HttpStatusCode.Conflict, "Non-approved review must return conflict.");

var services = new ServiceCollection();
services.AddReviewResolutionModule();
Assert(services.Any(x => x.ServiceType == typeof(IReviewResolutionReviewReader)), "Concrete review reader must be wired.");
Assert(services.Any(x => x.ServiceType == typeof(IReviewResolutionEvidenceMatcher)), "Concrete evidence matcher must be wired.");
Assert(services.Any(x => x.ServiceType == typeof(IReviewResolutionStore)), "Concrete durable resolution store must be wired.");

await app.DisposeAsync();
await notEligibleApp.DisposeAsync();
Console.WriteLine("Reconciliation review resolution composition, protected HTTP and idempotency scenarios: PASS");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FixedReviewReader(ReconciliationReviewItem? item) : IReviewResolutionReviewReader
{
    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(item?.ReviewId == reviewId ? item : null);
    }
}

sealed class FixedEvidenceMatcher(ReviewResolutionEvidence? evidence) : IReviewResolutionEvidenceMatcher
{
    public Task<ReviewResolutionEvidence?> FindMatchingEvidenceAsync(ReconciliationReviewItem review, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(evidence);
    }
}

sealed class InMemoryResolutionStore : IReviewResolutionStore
{
    private readonly Dictionary<Guid, ReviewResolutionRecord> values = new();
    public int Count => values.Count;
    public Task<ReviewResolutionRecord?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }
    public Task<bool> TryAddAsync(ReviewResolutionRecord resolution, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryAdd(resolution.ReviewId, resolution));
    }
}

sealed class HeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || string.IsNullOrWhiteSpace(raw))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim("sub", raw.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
