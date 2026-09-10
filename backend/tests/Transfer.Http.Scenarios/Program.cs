using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using AfriWallet.Wallet.Persistence;
using IdentityService.Api.Transfer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await TransferHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var ownerClient = CreateClient(fixture.App, fixture.OwnerId);
var otherClient = CreateClient(fixture.App, fixture.OtherOwnerId);

await RunAsync("internal transfer requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync("/api/v1/transfers/internal", fixture.Request(100, Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("invalid request returns 400", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/transfers/internal", fixture.Request(0, Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.ValidationError, "Expected TRANSFER_VALIDATION_ERROR.");
});

await RunAsync("foreign source wallet is hidden with 404", async () =>
{
    var request = new InternalTransferRequest(fixture.ForeignSourceWalletId, fixture.TargetWalletId, 100, Guid.NewGuid());
    var response = await ownerClient.PostAsJsonAsync("/api/v1/transfers/internal", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.NotFound, "Expected TRANSFER_NOT_FOUND.");
});

await RunAsync("missing target wallet returns 404", async () =>
{
    var request = new InternalTransferRequest(fixture.SourceWalletId, Guid.NewGuid(), 100, Guid.NewGuid());
    var response = await ownerClient.PostAsJsonAsync("/api/v1/transfers/internal", request);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.NotFound, "Expected TRANSFER_NOT_FOUND.");
});

var successfulCorrelation = Guid.NewGuid();
InternalTransferResponse? successfulTransfer = null;
await RunAsync("owned source executes transfer and persists balanced ledger journal", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/transfers/internal", fixture.Request(2_500, successfulCorrelation));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    successfulTransfer = await response.Content.ReadFromJsonAsync<InternalTransferResponse>();
    Assert(successfulTransfer is not null, "Transfer response is required.");
    Assert(successfulTransfer!.SourceWalletId == fixture.SourceWalletId, "Source wallet mismatch.");
    Assert(successfulTransfer.TargetWalletId == fixture.TargetWalletId, "Target wallet mismatch.");
    Assert(successfulTransfer.CurrencyCode == "XAF", "Currency mismatch.");
    Assert(successfulTransfer.AmountMinor == 2_500, "Amount mismatch.");
    Assert(successfulTransfer.CorrelationId == successfulCorrelation, "Correlation mismatch.");

    var journal = await fixture.GetJournalByCorrelationAsync(successfulCorrelation);
    Assert(journal is not null && journal.Id.Value == successfulTransfer.JournalEntryId, "Persisted journal mismatch.");
    Assert(journal!.Lines.Where(x => x.Side == LedgerSide.Debit).Sum(x => x.AmountMinor) == 2_500, "Debit total mismatch.");
    Assert(journal.Lines.Where(x => x.Side == LedgerSide.Credit).Sum(x => x.AmountMinor) == 2_500, "Credit total mismatch.");

    var sourceBalance = await fixture.ReadBalanceAsync(fixture.SourceAccountId, "XAF");
    var targetBalance = await fixture.ReadBalanceAsync(fixture.TargetAccountId, "XAF");
    Assert(sourceBalance.NetMinor == 7_500, $"Expected source net 7500, got {sourceBalance.NetMinor}.");
    Assert(targetBalance.NetMinor == 2_500, $"Expected target net 2500, got {targetBalance.NetMinor}.");
});

await RunAsync("duplicate correlation returns 409 without second journal", async () =>
{
    var response = await ownerClient.PostAsJsonAsync("/api/v1/transfers/internal", fixture.Request(2_500, successfulCorrelation));
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<TransferErrorResponse>();
    Assert(error?.Code == TransferErrorCode.Conflict, "Expected TRANSFER_CONFLICT.");
    Assert(await fixture.CountJournalsByCorrelationAsync(successfulCorrelation) == 1, "Duplicate correlation must not create a second journal.");
});

await RunAsync("other owner can execute from their own source wallet", async () =>
{
    var response = await otherClient.PostAsJsonAsync(
        "/api/v1/transfers/internal",
        new InternalTransferRequest(fixture.ForeignSourceWalletId, fixture.TargetWalletId, 100, Guid.NewGuid()));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200 for actual owner, got {(int)response.StatusCode}.");
});

await RunAsync("no alternative internal transfer routes are exposed", async () =>
{
    foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
    {
        using var request = new HttpRequestMessage(method, "/api/v1/transfers/internal");
        var response = await ownerClient.SendAsync(request);
        Assert(response.StatusCode == HttpStatusCode.MethodNotAllowed, $"Expected 405 for {method}, got {(int)response.StatusCode}.");
    }

    var collectionResponse = await ownerClient.PostAsJsonAsync("/api/v1/transfers", fixture.Request(100, Guid.NewGuid()));
    Assert(collectionResponse.StatusCode == HttpStatusCode.NotFound, $"Expected 404 for alternate POST route, got {(int)collectionResponse.StatusCode}.");
});

Console.WriteLine("Transfer HTTP security & integration scenarios passed.");

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

sealed class TransferHttpFixture : IAsyncDisposable
{
    private readonly SqliteConnection walletConnection;
    private readonly SqliteConnection ledgerConnection;

    private TransferHttpFixture(
        WebApplication app,
        SqliteConnection walletConnection,
        SqliteConnection ledgerConnection,
        Guid ownerId,
        Guid otherOwnerId,
        Guid sourceWalletId,
        Guid targetWalletId,
        Guid foreignSourceWalletId,
        AccountId sourceAccountId,
        AccountId targetAccountId)
    {
        App = app;
        this.walletConnection = walletConnection;
        this.ledgerConnection = ledgerConnection;
        OwnerId = ownerId;
        OtherOwnerId = otherOwnerId;
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        ForeignSourceWalletId = foreignSourceWalletId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
    }

    public WebApplication App { get; }
    public Guid OwnerId { get; }
    public Guid OtherOwnerId { get; }
    public Guid SourceWalletId { get; }
    public Guid TargetWalletId { get; }
    public Guid ForeignSourceWalletId { get; }
    public AccountId SourceAccountId { get; }
    public AccountId TargetAccountId { get; }

    public InternalTransferRequest Request(long amountMinor, Guid correlationId) =>
        new(SourceWalletId, TargetWalletId, amountMinor, correlationId);

    public static async Task<TransferHttpFixture> CreateAsync()
    {
        var walletConnection = new SqliteConnection("Data Source=:memory:");
        var ledgerConnection = new SqliteConnection("Data Source=:memory:");
        await walletConnection.OpenAsync();
        await ledgerConnection.OpenAsync();

        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var sourceWalletId = Guid.NewGuid();
        var targetWalletId = Guid.NewGuid();
        var foreignSourceWalletId = Guid.NewGuid();
        var sourceAccountId = AccountId.New();
        var targetAccountId = AccountId.New();
        var foreignAccountId = AccountId.New();

        var mappings = new Dictionary<Guid, AccountId>
        {
            [sourceWalletId] = sourceAccountId,
            [targetWalletId] = targetAccountId,
            [foreignSourceWalletId] = foreignAccountId
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddDbContext<WalletDbContext>(options => options.UseSqlite(walletConnection));
        builder.Services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(ledgerConnection));
        builder.Services.AddScoped<IWalletRepository, EfWalletRepository>();
        builder.Services.AddScoped<IJournalRepository, EfJournalRepository>();
        builder.Services.AddScoped<ILedgerJournalReader, EfLedgerJournalReader>();
        builder.Services.AddSingleton<BalanceProjectionService>();
        builder.Services.AddScoped<LedgerBackedBalanceReadService>();
        builder.Services.AddSingleton<IWalletLedgerAccountResolver>(new ConfiguredWalletLedgerAccountResolver(mappings));
        builder.Services.AddSingleton<ITransferFundsAvailabilityPolicy, NonNegativeNetTransferFundsAvailabilityPolicy>();
        builder.Services.AddScoped<ITransferWalletReader, WalletRegistryTransferWalletReader>();
        builder.Services.AddScoped<ITransferBalanceReader, BalanceProjectionTransferBalanceReader>();
        builder.Services.AddScoped<ITransferLedgerPort, UniversalLedgerTransferLedgerPort>();
        builder.Services.AddScoped<InternalTransferPlanningService>();
        builder.Services.AddScoped<InternalTransferOrchestrationService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTransferEndpoints();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var wallets = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            var ledger = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await wallets.Database.MigrateAsync();
            await ledger.Database.MigrateAsync();

            var walletRepository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            await walletRepository.AddAsync(Wallet.Create(WalletId.From(sourceWalletId), ownerId, Currency.Create("XAF"), null, DateTimeOffset.UtcNow));
            await walletRepository.AddAsync(Wallet.Create(WalletId.From(targetWalletId), Guid.NewGuid(), Currency.Create("XAF"), null, DateTimeOffset.UtcNow));
            await walletRepository.AddAsync(Wallet.Create(WalletId.From(foreignSourceWalletId), otherOwnerId, Currency.Create("XAF"), null, DateTimeOffset.UtcNow));

            var journalRepository = scope.ServiceProvider.GetRequiredService<IJournalRepository>();
            await journalRepository.AddAsync(SeedCredit(sourceAccountId, 10_000));
            await journalRepository.AddAsync(SeedCredit(foreignAccountId, 1_000));
        }

        await app.StartAsync();
        return new TransferHttpFixture(app, walletConnection, ledgerConnection, ownerId, otherOwnerId, sourceWalletId, targetWalletId, foreignSourceWalletId, sourceAccountId, targetAccountId);
    }

    private static JournalEntry SeedCredit(AccountId accountId, long amountMinor)
    {
        var counterparty = AccountId.New();
        return JournalEntry.Create(
            JournalEntryId.New(),
            "XAF",
            $"TRANSFER-HTTP-SEED-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                new LedgerLine(counterparty, LedgerSide.Debit, amountMinor, "Seed counterparty"),
                new LedgerLine(accountId, LedgerSide.Credit, amountMinor, "Seed available funds")
            ]);
    }

    public async Task<JournalEntry?> GetJournalByCorrelationAsync(Guid correlationId)
    {
        await using var scope = App.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IJournalRepository>().GetByCorrelationIdAsync(correlationId);
    }

    public async Task<int> CountJournalsByCorrelationAsync(Guid correlationId) =>
        await GetJournalByCorrelationAsync(correlationId) is null ? 0 : 1;

    public async Task<AccountBalanceSnapshot> ReadBalanceAsync(AccountId accountId, string currencyCode)
    {
        await using var scope = App.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LedgerBackedBalanceReadService>().ReadAsync(new BalanceKey(accountId, currencyCode));
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        await walletConnection.DisposeAsync();
        await ledgerConnection.DisposeAsync();
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
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
