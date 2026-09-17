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
using Reconciliation.Application.Matching;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;
using Reconciliation.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var repository = new InMemoryReconciliationReviewRepository();
var queueService = new ReconciliationReviewQueueService();
var service = new ReconciliationReviewApplicationService(repository, queueService);
var queuedAt = DateTime.UtcNow.AddMinutes(-5);
var batch = new AutomaticCandidateClassificationBatch(
    "partner-http",
    queuedAt.AddHours(-1),
    queuedAt.AddMinutes(-1),
    [new AutomaticCandidateClassification("i-1", "e-1", ReconciliationMatchType.Partial, 70, -50, TimeSpan.FromMinutes(1))]);
var queue = await service.EnqueueAsync(batch, queuedAt);
var reviewId = queue.Items.Single().ReviewId;

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services.AddSingleton<IReconciliationReviewRepository>(repository);
builder.Services.AddSingleton(queueService);
builder.Services.AddSingleton<ReconciliationReviewApplicationService>();
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapReconciliationReviewEndpoints();
await app.StartAsync();

var anonymous = app.GetTestClient();
var anonymousResponse = await anonymous.GetAsync("/api/v1/reconciliation/reviews/?partnerId=partner-http");
Assert(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "Review queue must require authentication.");

var reviewerId = Guid.NewGuid();
var client = app.GetTestClient();
client.DefaultRequestHeaders.Add("X-Test-User", reviewerId.ToString());
var list = await client.GetAsync("/api/v1/reconciliation/reviews/?partnerId=partner-http&status=PendingReview");
Assert(list.StatusCode == HttpStatusCode.OK, "Authenticated reviewer must list queue.");
var listed = await list.Content.ReadFromJsonAsync<ReconciliationReviewResponse[]>();
Assert(listed is { Length: 1 } && listed[0].ReviewId == reviewId, "Queue must expose stored review item.");

var invalidDecision = await client.PostAsJsonAsync(
    $"/api/v1/reconciliation/reviews/{reviewId}/decision",
    new ReconciliationReviewDecisionRequest("Rejected", null));
Assert(invalidDecision.StatusCode == HttpStatusCode.BadRequest, "Reject without reason must return 400.");

var decision = await client.PostAsJsonAsync(
    $"/api/v1/reconciliation/reviews/{reviewId}/decision",
    new ReconciliationReviewDecisionRequest("Approved", null));
Assert(decision.StatusCode == HttpStatusCode.OK, "Approved decision must succeed.");
var decided = await decision.Content.ReadFromJsonAsync<ReconciliationReviewResponse>();
Assert(decided?.Status == "Approved", "Decision response must be approved.");
Assert(decided?.ReviewerId == reviewerId.ToString(), "Reviewer must be derived from authenticated sub claim.");

var replay = await client.PostAsJsonAsync(
    $"/api/v1/reconciliation/reviews/{reviewId}/decision",
    new ReconciliationReviewDecisionRequest("Rejected", "change"));
Assert(replay.StatusCode == HttpStatusCode.Conflict, "Terminal review must reject second decision.");

var missing = await client.GetAsync($"/api/v1/reconciliation/reviews/{Guid.NewGuid()}");
Assert(missing.StatusCode == HttpStatusCode.NotFound, "Unknown review must return 404.");
var badStatus = await client.GetAsync("/api/v1/reconciliation/reviews/?partnerId=partner-http&status=bogus");
Assert(badStatus.StatusCode == HttpStatusCode.BadRequest, "Unknown status must return 400.");

await app.DisposeAsync();
Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-3 protected review HTTP scenarios: PASS");

sealed class HeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var value) || string.IsNullOrWhiteSpace(value))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim("sub", value.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
