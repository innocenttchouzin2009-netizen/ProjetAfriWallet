using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reconciliation.Api.Resolution;
using Reconciliation.Application.Matching;
using Reconciliation.Application.Resolution;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Resolutions;
using Reconciliation.Infrastructure.Resolutions;

await using var fixture = await ResolutionHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var resolverClient = CreateClient(fixture.App, fixture.ResolverId);
var otherClient = CreateClient(fixture.App, "resolver-other");

await RunAsync("resolution creation requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("unknown review is hidden with 404", async () =>
{
    var response = await resolverClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{Guid.NewGuid()}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("non-final review cannot be resolved", async () =>
{
    var response = await resolverClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PendingReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
});

await RunAsync("invalid disposition is rejected", async () =>
{
    var response = await resolverClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution",
        fixture.ValidRequest() with { Disposition = "UnknownDisposition" });
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated resolver creates durable resolution", async () =>
{
    var response = await resolverClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");

    var body = await response.Content.ReadFromJsonAsync<ReconciliationResolutionResponse>();
    Assert(body is not null, "Resolution response is required.");
    Assert(body!.ReviewId == fixture.ApprovedReviewId, "Review id mismatch.");
    Assert(body.ResolvedBy == fixture.ResolverId, "ResolvedBy must come from authenticated sub claim.");
    Assert(body.Disposition == ReconciliationResolutionDisposition.RecordCorrected.ToString(), "Disposition mismatch.");
    Assert(fixture.Resolutions.AddCalls == 1, "Resolution must be stored exactly once.");
    Assert(fixture.Resolutions.Audit.Count == 1, "Resolution creation must append exactly one audit entry.");
});

await RunAsync("resolution read is protected and visible after creation", async () =>
{
    var anonymousResponse = await anonymous.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution");
    Assert(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "Anonymous read must return 401.");

    var response = await resolverClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
});

await RunAsync("audit read is protected and scoped to existing resolution", async () =>
{
    var response = await resolverClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution/audit");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var audit = await response.Content.ReadFromJsonAsync<ReconciliationResolutionAuditResponse[]>();
    Assert(audit is { Length: 1 }, "Exactly one audit entry is expected.");

    var missing = await resolverClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{Guid.NewGuid()}/resolution/audit");
    Assert(missing.StatusCode == HttpStatusCode.NotFound, "Unknown resolution audit must return 404.");
});

await RunAsync("identical replay is idempotent", async () =>
{
    var response = await resolverClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200 replay, got {(int)response.StatusCode}.");
    Assert(fixture.Resolutions.AddCalls == 1, "Idempotent replay must not add a second resolution.");
    Assert(fixture.Resolutions.Audit.Count == 1, "Idempotent replay must not append a second audit event.");
});

await RunAsync("different authenticated resolver cannot overwrite resolution", async () =>
{
    var response = await otherClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.ApprovedReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    Assert(fixture.Resolutions.AddCalls == 1, "Conflict must not overwrite durable resolution.");
});

await RunAsync("composition registers SQLite repository and audit reader", async () =>
{
    var services = new ServiceCollection();
    services.AddReconciliationResolutionPersistence("Data Source=:memory:");
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();

    var repository = scope.ServiceProvider.GetRequiredService<IReconciliationResolutionRepository>();
    var auditReader = scope.ServiceProvider.GetRequiredService<IReconciliationResolutionAuditReader>();
    Assert(repository is EfReconciliationResolutionRepository, "SQLite repository adapter must be registered.");
    Assert(ReferenceEquals(repository, auditReader), "Repository and audit reader must share the scoped adapter.");
});

Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 protected HTTP API and composition scenarios: PASS");

static HttpClient CreateClient(WebApplication app, string userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId);
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

sealed class ResolutionHttpFixture : IAsyncDisposable
{
    private ResolutionHttpFixture(
        WebApplication app,
        FakeResolutionRepository resolutions,
        string resolverId,
        Guid approvedReviewId,
        Guid pendingReviewId)
    {
        App = app;
        Resolutions = resolutions;
        ResolverId = resolverId;
        ApprovedReviewId = approvedReviewId;
        PendingReviewId = pendingReviewId;
    }

    public WebApplication App { get; }
    public FakeResolutionRepository Resolutions { get; }
    public string ResolverId { get; }
    public Guid ApprovedReviewId { get; }
    public Guid PendingReviewId { get; }

    public CreateReconciliationResolutionRequest ValidRequest() =>
        new("RecordCorrected", "statement corrected and replayed", "evidence://case/42");

    public static async Task<ResolutionHttpFixture> CreateAsync()
    {
        const string resolverId = "resolver-42";
        var approvedReviewId = Guid.NewGuid();
        var pendingReviewId = Guid.NewGuid();
        var decidedAt = DateTime.UtcNow.AddMinutes(-10);

        var reviews = new FakeReviewRepository(
        [
            Review(approvedReviewId, ReconciliationReviewStatus.Approved, decidedAt),
            Review(pendingReviewId, ReconciliationReviewStatus.PendingReview, null)
        ]);
        var resolutions = new FakeResolutionRepository();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IReconciliationReviewRepository>(reviews);
        builder.Services.AddSingleton<IReconciliationResolutionRepository>(resolutions);
        builder.Services.AddSingleton<IReconciliationResolutionAuditReader>(resolutions);
        builder.Services.AddScoped<ReconciliationResolutionApplicationService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapReconciliationResolutionEndpoints();
        await app.StartAsync();

        return new ResolutionHttpFixture(app, resolutions, resolverId, approvedReviewId, pendingReviewId);
    }

    private static ReconciliationReviewItem Review(
        Guid reviewId,
        ReconciliationReviewStatus status,
        DateTime? decidedAtUtc) =>
        new(
            reviewId,
            "partner-one",
            "internal-42",
            "external-42",
            ReconciliationMatchType.Partial,
            80,
            10,
            TimeSpan.FromSeconds(5),
            DateTime.UtcNow.AddMinutes(-20),
            status,
            status == ReconciliationReviewStatus.PendingReview ? null : "reviewer-1",
            status == ReconciliationReviewStatus.PendingReview ? null : "review complete",
            decidedAtUtc);

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class FakeReviewRepository(IEnumerable<ReconciliationReviewItem> items) : IReconciliationReviewRepository
{
    private readonly Dictionary<Guid, ReconciliationReviewItem> values = items.ToDictionary(x => x.ReviewId);

    public Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(reviewId, out var item) ? item : null);
    }

    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReconciliationReviewItem>>(
            values.Values.Where(x =>
                x.PartnerId == partnerId &&
                (status is null || x.Status == status.Value)).ToArray());

    public Task<bool> TryReplaceAsync(
        ReconciliationReviewItem item,
        ReconciliationReviewStatus expectedStatus,
        CancellationToken cancellationToken = default)
    {
        if (!values.TryGetValue(item.ReviewId, out var current) || current.Status != expectedStatus)
            return Task.FromResult(false);
        values[item.ReviewId] = item;
        return Task.FromResult(true);
    }
}

sealed class FakeResolutionRepository :
    IReconciliationResolutionRepository,
    IReconciliationResolutionAuditReader
{
    private readonly Dictionary<Guid, ReconciliationResolution> values = new();
    public List<ReconciliationResolutionAuditEntry> Audit { get; } = [];
    public int AddCalls { get; private set; }

    public Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(reviewId, out var resolution) ? resolution : null);
    }

    public Task AddAsync(
        ReconciliationResolution resolution,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (values.ContainsKey(resolution.ReviewId))
            throw new InvalidOperationException("Review already has a durable reconciliation resolution.");

        values.Add(resolution.ReviewId, resolution);
        Audit.Add(new ReconciliationResolutionAuditEntry(
            Guid.NewGuid(),
            resolution.ResolutionId,
            resolution.ReviewId,
            resolution.Disposition,
            resolution.ResolvedBy,
            resolution.Rationale,
            resolution.EvidenceReference,
            resolution.ResolvedAtUtc));
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReconciliationResolutionAuditEntry>> ListByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ReconciliationResolutionAuditEntry>>(
            Audit.Where(x => x.ReviewId == reviewId).ToArray());
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
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) ||
            string.IsNullOrWhiteSpace(raw.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", raw.ToString())],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme.Name)));
    }
}
