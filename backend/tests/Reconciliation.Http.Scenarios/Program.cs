using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Reconciliation.Application;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.Reconciliation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await ReconciliationHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var sourceClient = CreateClient(fixture.App, fixture.SourceOwnerId);
var targetClient = CreateClient(fixture.App, fixture.TargetOwnerId);
var outsiderClient = CreateClient(fixture.App, Guid.NewGuid());

await RunAsync("reconciliation requires authentication", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/reconciliation/transfers/by-correlation/{fixture.CorrelationId}");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("source owner can reconcile by correlation id", async () =>
{
    var response = await sourceClient.GetAsync($"/api/v1/reconciliation/transfers/by-correlation/{fixture.CorrelationId}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var receipt = await response.Content.ReadFromJsonAsync<ReconciliationTransferResponse>();
    Assert(receipt is not null, "Receipt is required.");
    Assert(receipt!.TransferId == fixture.TransferId, "Transfer id mismatch.");
    Assert(receipt.JournalEntryId == fixture.JournalEntryId, "Journal id mismatch.");
    Assert(receipt.SourceWalletId == fixture.SourceWalletId, "Source wallet mismatch.");
    Assert(receipt.TargetWalletId == fixture.TargetWalletId, "Target wallet mismatch.");
    Assert(receipt.CurrencyCode == "XAF" && receipt.AmountMinor == 25_000, "Transfer amount/currency mismatch.");
});

await RunAsync("target owner can reconcile by journal entry id", async () =>
{
    var response = await targetClient.GetAsync($"/api/v1/reconciliation/transfers/by-journal/{fixture.JournalEntryId}");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
});

await RunAsync("unrelated user is hidden with 404", async () =>
{
    var response = await outsiderClient.GetAsync($"/api/v1/reconciliation/transfers/by-correlation/{fixture.CorrelationId}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<ReconciliationErrorResponse>();
    Assert(error?.Code == ReconciliationErrorCode.NotFound, "Expected RECON_NOT_FOUND.");
});

await RunAsync("unknown transfer is 404", async () =>
{
    var response = await sourceClient.GetAsync($"/api/v1/reconciliation/transfers/by-correlation/{Guid.NewGuid()}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("non-transfer journal is hidden with 404", async () =>
{
    var response = await sourceClient.GetAsync($"/api/v1/reconciliation/transfers/by-journal/{fixture.NonTransferJournalEntryId}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Console.WriteLine("AFW-BE-RECON-1 protected reconciliation HTTP scenarios: PASS");

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

sealed class ReconciliationHttpFixture : IAsyncDisposable
{
    private ReconciliationHttpFixture(
        WebApplication app,
        Guid sourceOwnerId,
        Guid targetOwnerId,
        Guid sourceWalletId,
        Guid targetWalletId,
        Guid transferId,
        Guid correlationId,
        Guid journalEntryId,
        Guid nonTransferJournalEntryId)
    {
        App = app;
        SourceOwnerId = sourceOwnerId;
        TargetOwnerId = targetOwnerId;
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        TransferId = transferId;
        CorrelationId = correlationId;
        JournalEntryId = journalEntryId;
        NonTransferJournalEntryId = nonTransferJournalEntryId;
    }

    public WebApplication App { get; }
    public Guid SourceOwnerId { get; }
    public Guid TargetOwnerId { get; }
    public Guid SourceWalletId { get; }
    public Guid TargetWalletId { get; }
    public Guid TransferId { get; }
    public Guid CorrelationId { get; }
    public Guid JournalEntryId { get; }
    public Guid NonTransferJournalEntryId { get; }

    public static async Task<ReconciliationHttpFixture> CreateAsync()
    {
        var sourceOwnerId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var sourceWalletId = Guid.NewGuid();
        var targetWalletId = Guid.NewGuid();
        var debitAccount = new AccountId(Guid.NewGuid());
        var creditAccount = new AccountId(Guid.NewGuid());
        var transferId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var journalId = AfriWallet.Ledger.Domain.JournalEntryId.New();
        var nonTransferJournalId = AfriWallet.Ledger.Domain.JournalEntryId.New();
        var now = new DateTimeOffset(2026, 9, 13, 7, 30, 0, TimeSpan.Zero);

        var sourceWallet = Wallet.Create(WalletId.From(sourceWalletId), sourceOwnerId, Currency.Create("XAF"), null, now);
        var targetWallet = Wallet.Create(WalletId.From(targetWalletId), targetOwnerId, Currency.Create("XAF"), null, now);
        var wallets = new InMemoryWalletRepository([sourceWallet, targetWallet]);
        var transferWallets = new InMemoryTransferWalletReader([
            new TransferWalletSnapshot(sourceWalletId, debitAccount, "XAF", true),
            new TransferWalletSnapshot(targetWalletId, creditAccount, "XAF", true)
        ]);

        var transferJournal = JournalEntry.Create(
            journalId,
            "XAF",
            $"TRF-{transferId:N}",
            correlationId,
            now,
            [
                new LedgerLine(debitAccount, LedgerSide.Debit, 25_000, "Internal transfer debit"),
                new LedgerLine(creditAccount, LedgerSide.Credit, 25_000, "Internal transfer credit")
            ]);

        var nonTransferJournal = JournalEntry.Create(
            nonTransferJournalId,
            "XAF",
            "MANUAL-ENTRY",
            Guid.NewGuid(),
            now.AddMinutes(1),
            [
                new LedgerLine(debitAccount, LedgerSide.Debit, 100, "Manual debit"),
                new LedgerLine(creditAccount, LedgerSide.Credit, 100, "Manual credit")
            ]);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"Transfer:WalletLedgerAccounts:{sourceWalletId}"] = debitAccount.Value.ToString(),
            [$"Transfer:WalletLedgerAccounts:{targetWalletId}"] = creditAccount.Value.ToString()
        });

        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IWalletRepository>(wallets);
        builder.Services.AddSingleton<ITransferWalletReader>(transferWallets);
        builder.Services.AddSingleton<IJournalRepository>(new InMemoryJournalRepository([transferJournal, nonTransferJournal]));
        builder.Services.AddReconciliationModule(builder.Configuration);

        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ITransferLedgerPort)))
        {
            throw new InvalidOperationException("Reconciliation composition must not register a transfer ledger writer.");
        }

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapReconciliationEndpoints();
        await app.StartAsync();

        return new ReconciliationHttpFixture(
            app,
            sourceOwnerId,
            targetOwnerId,
            sourceWalletId,
            targetWalletId,
            transferId,
            correlationId,
            journalId.Value,
            nonTransferJournalId.Value);
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryTransferWalletReader(IEnumerable<TransferWalletSnapshot> seed) : ITransferWalletReader
{
    private readonly Dictionary<Guid, TransferWalletSnapshot> wallets = seed.ToDictionary(x => x.WalletId);

    public Task<TransferWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets.TryGetValue(walletId, out var value);
        return Task.FromResult(value);
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> seed) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> wallets = seed.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(wallets.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets.TryGetValue(walletId.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(wallets.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}

sealed class InMemoryJournalRepository(IEnumerable<JournalEntry> seed) : IJournalRepository
{
    private readonly Dictionary<Guid, JournalEntry> byId = seed.ToDictionary(x => x.Id.Value);
    private readonly Dictionary<Guid, JournalEntry> byCorrelation = seed.ToDictionary(x => x.CorrelationId);

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(byCorrelation.ContainsKey(correlationId));

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("HTTP reconciliation scenarios are read-only.");

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.TryGetValue(journalEntryId.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
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
