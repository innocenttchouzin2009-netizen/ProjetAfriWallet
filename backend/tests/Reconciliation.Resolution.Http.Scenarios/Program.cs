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

await using var fixture = await ResolutionHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var partnerOneClient = CreateClient(fixture.App, "resolver-one", ["partner-one"]);
var partnerOneAuditor = CreateClient(fixture.App, "auditor-one", ["partner-one"], canAudit: true);
var partnerTwoClient = CreateClient(fixture.App, "resolver-two", ["partner-two"]);
var forgedGlobalClient = CreateClient(fixture.App, "forged-global", ["partner-one"], allPartners: true);
var adminClient = CreateClient(fixture.App, "resolver-admin", [], allPartners: true, administrator: true);

await RunAsync("resolution creation requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Anonymous create must return 401.");
});

await RunAsync("authorized partner resolver creates resolution", async () =>
{
    var response = await partnerOneClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<ReconciliationResolutionResponse>();
    Assert(body?.ResolvedBy == "resolver-one", "Resolver identity must come from authenticated scope.");
});

await RunAsync("ordinary resolver cannot read audit", async () =>
{
    var response = await partnerOneClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution/audit");
    Assert(response.StatusCode == HttpStatusCode.Forbidden, "Resolver without audit permission must return 403.");
    var error = await response.Content.ReadFromJsonAsync<ReconciliationResolutionError>();
    Assert(error?.Code == "RECONCILIATION_RESOLUTION_AUDIT_FORBIDDEN", "Dedicated audit denial code is required.");
});

await RunAsync("authorized partner auditor reads own audit trail", async () =>
{
    var response = await partnerOneAuditor.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution/audit");
    Assert(response.StatusCode == HttpStatusCode.OK, "Authorized auditor must receive 200.");
    var items = await response.Content.ReadFromJsonAsync<ReconciliationResolutionAuditResponse[]>();
    Assert(items is { Length: 1 }, "Resolution must expose exactly one append-only creation audit entry.");
    Assert(items[0].ResolvedBy == "resolver-one", "Audit must preserve original resolver identity.");
});

await RunAsync("foreign partner resolver is forbidden", async () =>
{
    var create = await partnerTwoClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution",
        fixture.ValidRequest());
    Assert(create.StatusCode == HttpStatusCode.Forbidden, "Foreign partner create must return 403.");

    var read = await partnerTwoClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution");
    Assert(read.StatusCode == HttpStatusCode.Forbidden, "Foreign partner read must return 403.");
});

await RunAsync("second scoped partner creates own resolution", async () =>
{
    var response = await partnerTwoClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerTwoReviewId}/resolution",
        fixture.ValidRequest() with { Disposition = "VarianceAccepted" });
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
});

await RunAsync("operational list is scope filtered", async () =>
{
    var response = await partnerOneClient.GetAsync("/api/v1/reconciliation/resolutions");
    Assert(response.StatusCode == HttpStatusCode.OK, "Scoped list must return 200.");
    var items = await response.Content.ReadFromJsonAsync<ReconciliationResolutionResponse[]>();
    Assert(items is { Length: 1 } && items[0].PartnerId == "partner-one",
        "Partner scope must filter operational list.");
});

await RunAsync("explicit forbidden partner query returns 403", async () =>
{
    var response = await partnerOneClient.GetAsync("/api/v1/reconciliation/resolutions?partnerId=partner-two");
    Assert(response.StatusCode == HttpStatusCode.Forbidden, "Explicit forbidden partner query must return 403.");
});

await RunAsync("all-partners claim without administrator privilege fails closed", async () =>
{
    var response = await forgedGlobalClient.GetAsync("/api/v1/reconciliation/resolutions?limit=10");
    Assert(response.StatusCode == HttpStatusCode.OK, "Forged global claim remains partner-scoped, not globally rejected.");
    var items = await response.Content.ReadFromJsonAsync<ReconciliationResolutionResponse[]>();
    Assert(items is { Length: 1 } && items[0].PartnerId == "partner-one",
        "All-partners claim must not widen scope without explicit administrator privilege.");
});

await RunAsync("administrator can query across partners and read audit", async () =>
{
    var response = await adminClient.GetAsync("/api/v1/reconciliation/resolutions?limit=10");
    Assert(response.StatusCode == HttpStatusCode.OK, "Admin operational list must return 200.");
    var items = await response.Content.ReadFromJsonAsync<ReconciliationResolutionResponse[]>();
    Assert(items is { Length: 2 }, "Admin scope must see both partner resolutions.");

    var variance = await adminClient.GetAsync("/api/v1/reconciliation/resolutions?disposition=VarianceAccepted");
    var filtered = await variance.Content.ReadFromJsonAsync<ReconciliationResolutionResponse[]>();
    Assert(variance.StatusCode == HttpStatusCode.OK && filtered is { Length: 1 } && filtered[0].PartnerId == "partner-two",
        "Disposition query must filter operational list.");

    var audit = await adminClient.GetAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerTwoReviewId}/resolution/audit");
    Assert(audit.StatusCode == HttpStatusCode.OK, "Administrator must be able to read cross-partner audit.");
});

await RunAsync("identical replay remains idempotent", async () =>
{
    var response = await partnerOneClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{fixture.PartnerOneReviewId}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.OK, "Equivalent replay must return 200.");
    Assert(fixture.Resolutions.AddCalls == 2, "Replay must not add another resolution.");
});

await RunAsync("unknown review remains 404 for authorized actor", async () =>
{
    var response = await partnerOneClient.PostAsJsonAsync(
        $"/api/v1/reconciliation/reviews/{Guid.NewGuid()}/resolution",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Unknown review must return 404.");
});

Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 final administrative and audit HTTP scenarios: PASS");

static HttpClient CreateClient(
    WebApplication app,
    string userId,
    string[] partners,
    bool allPartners = false,
    bool administrator = false,
    bool canAudit = false)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId);
    foreach (var partner in partners)
        client.DefaultRequestHeaders.Add("X-Test-Partner", partner);
    if (allPartners)
        client.DefaultRequestHeaders.Add("X-Test-All-Partners", "true");
    if (administrator)
        client.DefaultRequestHeaders.Add("X-Test-Administrator", "true");
    if (canAudit)
        client.DefaultRequestHeaders.Add("X-Test-Audit", "true");
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
        Guid partnerOneReviewId,
        Guid partnerTwoReviewId)
    {
        App = app;
        Resolutions = resolutions;
        PartnerOneReviewId = partnerOneReviewId;
        PartnerTwoReviewId = partnerTwoReviewId;
    }

    public WebApplication App { get; }
    public FakeResolutionRepository Resolutions { get; }
    public Guid PartnerOneReviewId { get; }
    public Guid PartnerTwoReviewId { get; }

    public CreateReconciliationResolutionRequest ValidRequest() =>
        new("RecordCorrected", "statement corrected and replayed", "evidence://case/42");

    public static async Task<ResolutionHttpFixture> CreateAsync()
    {
        var one = Guid.NewGuid();
        var two = Guid.NewGuid();
        var decidedAt = DateTime.UtcNow.AddMinutes(-10);
        var reviews = new FakeReviewRepository([
            Review(one, "partner-one", decidedAt),
            Review(two, "partner-two", decidedAt)
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

        return new ResolutionHttpFixture(app, resolutions, one, two);
    }

    private static ReconciliationReviewItem Review(Guid id, string partnerId, DateTime decidedAt) =>
        new(
            id,
            partnerId,
            $"internal-{id:N}",
            $"external-{id:N}",
            ReconciliationMatchType.Partial,
            80,
            10,
            TimeSpan.FromSeconds(5),
            decidedAt.AddMinutes(-10),
            ReconciliationReviewStatus.Approved,
            "reviewer-1",
            "review complete",
            decidedAt);

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class FakeReviewRepository(IEnumerable<ReconciliationReviewItem> items) : IReconciliationReviewRepository
{
    private readonly Dictionary<Guid, ReconciliationReviewItem> values = items.ToDictionary(x => x.ReviewId);
    public Task AddQueueAsync(ReconciliationReviewQueue queue, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(reviewId, out var item) ? item : null);
    }
    public Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(string partnerId, ReconciliationReviewStatus? status = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReconciliationReviewItem>>(values.Values.Where(x => x.PartnerId == partnerId).ToArray());
    public Task<bool> TryReplaceAsync(ReconciliationReviewItem item, ReconciliationReviewStatus expectedStatus, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

sealed class FakeResolutionRepository : IReconciliationResolutionRepository, IReconciliationResolutionAuditReader
{
    private readonly Dictionary<Guid, ReconciliationResolution> values = new();
    public List<ReconciliationResolutionAuditEntry> Audit { get; } = [];
    public int AddCalls { get; private set; }

    public Task<ReconciliationResolution?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(values.TryGetValue(reviewId, out var resolution) ? resolution : null);
    }

    public Task<IReadOnlyList<ReconciliationResolution>> ListAsync(
        ReconciliationResolutionRepositoryQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<ReconciliationResolution> source = values.Values;
        if (query.PartnerIds is { Count: > 0 })
            source = source.Where(x => query.PartnerIds.Contains(x.PartnerId));
        if (query.Disposition is not null)
            source = source.Where(x => x.Disposition == query.Disposition.Value);
        if (!string.IsNullOrWhiteSpace(query.ResolvedBy))
            source = source.Where(x => x.ResolvedBy == query.ResolvedBy);
        if (query.ResolvedFromUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc >= query.ResolvedFromUtc.Value);
        if (query.ResolvedToUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc <= query.ResolvedToUtc.Value);
        return Task.FromResult<IReadOnlyList<ReconciliationResolution>>(
            source.OrderByDescending(x => x.ResolvedAtUtc).Take(query.Limit).ToArray());
    }

    public Task AddAsync(ReconciliationResolution resolution, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.TryAdd(resolution.ReviewId, resolution))
            throw new InvalidOperationException("Review already has a durable reconciliation resolution.");
        Audit.Add(new ReconciliationResolutionAuditEntry(
            Guid.NewGuid(), resolution.ResolutionId, resolution.ReviewId, resolution.Disposition,
            resolution.ResolvedBy, resolution.Rationale, resolution.EvidenceReference, resolution.ResolvedAtUtc));
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
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || string.IsNullOrWhiteSpace(raw.ToString()))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", raw.ToString()) };
        foreach (var partner in Request.Headers["X-Test-Partner"])
            claims.Add(new Claim(ReconciliationResolutionAccessScopeFactory.PartnerClaim, partner ?? string.Empty));

        if (Request.Headers.TryGetValue("X-Test-All-Partners", out var all) &&
            string.Equals(all.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ReconciliationResolutionAccessScopeFactory.AllPartnersClaim, "true"));
        }

        if (Request.Headers.TryGetValue("X-Test-Administrator", out var admin) &&
            string.Equals(admin.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ReconciliationResolutionAccessScopeFactory.AdministratorClaim, "true"));
        }

        if (Request.Headers.TryGetValue("X-Test-Audit", out var audit) &&
            string.Equals(audit.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ReconciliationResolutionAccessScopeFactory.AuditClaim, "true"));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
