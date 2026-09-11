using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

var eur = Currency.Create("eur");
var expectedWalletId = WalletId.New();
var afWalLookup = new RecordingAfWalIdLookup(expectedWalletId);
var qrLookup = new RecordingQrLookup(expectedWalletId);
var resolutionService = new RecipientResolutionService(afWalLookup, qrLookup);

var afWalReference = RecipientReference.FromAfWalId("  leaty.237  ");
Assert(afWalReference.Kind == RecipientReferenceKind.AfWalId, "AfWal ID kind");
Assert(afWalReference.Value == "leaty.237", "AfWal ID trim normalization");

var afWalResult = await resolutionService.ResolveAsync(new ResolveRecipientRequest(afWalReference, eur));
Assert(afWalResult.Status == RecipientResolutionStatus.Success, "AfWal ID resolution succeeds");
Assert(afWalResult.Recipient?.WalletId == expectedWalletId, "AfWal ID resolves wallet");
Assert(afWalLookup.LastCurrency?.Code == "EUR", "currency forwarded to AfWal lookup");
Assert(qrLookup.Calls == 0, "QR lookup not used for AfWal ID");

var qrReference = RecipientReference.FromQrToken("opaque-recipient-token");
var qrResult = await resolutionService.ResolveAsync(new ResolveRecipientRequest(qrReference, eur));
Assert(qrResult.Status == RecipientResolutionStatus.Success, "QR resolution succeeds");
Assert(qrLookup.Calls == 1, "QR lookup used once");
Assert(afWalLookup.Calls == 1, "AfWal lookup not reused for QR");

var missingResolutionService = new RecipientResolutionService(new RecordingAfWalIdLookup(null), new RecordingQrLookup(null));
var missing = await missingResolutionService.ResolveAsync(new ResolveRecipientRequest(RecipientReference.FromAfWalId("unknown"), eur));
Assert(missing.Status == RecipientResolutionStatus.NotFound && missing.Recipient is null, "unknown recipient is not found");

AssertThrows<ArgumentException>(() => RecipientReference.FromAfWalId("   "), "blank AfWal ID rejected");
AssertThrows<ArgumentException>(() => RecipientReference.FromAfWalId("bad id"), "AfWal ID whitespace rejected");
AssertThrows<ArgumentException>(() => RecipientReference.FromQrToken(" token "), "QR surrounding whitespace rejected");

var sourceWalletId = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var requestedAtUtc = DateTimeOffset.UtcNow;
var transferPort = new RecordingP2PTransferPort();
var p2pService = new P2PTransferOrchestrationService(resolutionService, transferPort);

var p2pResult = await p2pService.ExecuteAsync(new ExecuteP2PTransferCommand(
    sourceWalletId,
    RecipientReference.FromAfWalId("leaty.237"),
    eur,
    25_000,
    correlationId,
    requestedAtUtc));

Assert(p2pResult.Status == P2PTransferExecutionStatus.Success, "P2P transfer succeeds after recipient resolution");
Assert(p2pResult.Recipient?.WalletId == expectedWalletId, "P2P returns resolved recipient");
Assert(p2pResult.Receipt is not null, "P2P returns transfer receipt");
Assert(transferPort.Calls == 1, "P2P delegates exactly once to Transfer port");
Assert(transferPort.LastSourceWalletId == sourceWalletId, "source wallet forwarded to Transfer port");
Assert(transferPort.LastTargetWalletId == expectedWalletId.Value, "resolved target wallet forwarded to Transfer port");
Assert(transferPort.LastAmountMinor == 25_000, "amount forwarded to Transfer port");
Assert(transferPort.LastCorrelationId == correlationId, "correlation id forwarded to Transfer port");
Assert(transferPort.LastRequestedAtUtc == requestedAtUtc, "UTC timestamp forwarded to Transfer port");

var untouchedTransferPort = new RecordingP2PTransferPort();
var notFoundP2PService = new P2PTransferOrchestrationService(missingResolutionService, untouchedTransferPort);
var recipientNotFound = await notFoundP2PService.ExecuteAsync(new ExecuteP2PTransferCommand(
    sourceWalletId,
    RecipientReference.FromAfWalId("unknown"),
    eur,
    1_000,
    Guid.NewGuid(),
    DateTimeOffset.UtcNow));
Assert(recipientNotFound.Status == P2PTransferExecutionStatus.RecipientNotFound, "P2P exposes recipient-not-found result");
Assert(untouchedTransferPort.Calls == 0, "Transfer port is not called when recipient is missing");

await AssertThrowsAsync<ArgumentException>(() => p2pService.ExecuteAsync(new ExecuteP2PTransferCommand(
    Guid.Empty,
    RecipientReference.FromAfWalId("leaty.237"),
    eur,
    1_000,
    Guid.NewGuid(),
    DateTimeOffset.UtcNow)), "empty source wallet rejected before transfer");

await AssertThrowsAsync<ArgumentOutOfRangeException>(() => p2pService.ExecuteAsync(new ExecuteP2PTransferCommand(
    sourceWalletId,
    RecipientReference.FromAfWalId("leaty.237"),
    eur,
    0,
    Guid.NewGuid(),
    DateTimeOffset.UtcNow)), "non-positive P2P amount rejected");

await AssertThrowsAsync<ArgumentException>(() => p2pService.ExecuteAsync(new ExecuteP2PTransferCommand(
    sourceWalletId,
    RecipientReference.FromAfWalId("leaty.237"),
    eur,
    1_000,
    Guid.Empty,
    DateTimeOffset.UtcNow)), "empty P2P correlation id rejected");

await AssertThrowsAsync<ArgumentException>(() => p2pService.ExecuteAsync(new ExecuteP2PTransferCommand(
    sourceWalletId,
    RecipientReference.FromAfWalId("leaty.237"),
    eur,
    1_000,
    Guid.NewGuid(),
    DateTimeOffset.Now)), "non-UTC P2P timestamp rejected");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(() => p2pService.ExecuteAsync(
    new ExecuteP2PTransferCommand(
        sourceWalletId,
        RecipientReference.FromAfWalId("cancelled"),
        eur,
        1_000,
        Guid.NewGuid(),
        DateTimeOffset.UtcNow),
    cts.Token), "P2P cancellation propagated before resolution/transfer");

Console.WriteLine("AFW-BE-P2P-1 recipient resolution and P2P transfer orchestration scenarios: PASS");

static void Assert(bool condition, string scenario)
{
    if (!condition) throw new InvalidOperationException($"Scenario failed: {scenario}");
    Console.WriteLine($"PASS: {scenario}");
}

static void AssertThrows<TException>(Action action, string scenario) where TException : Exception
{
    try { action(); }
    catch (TException) { Console.WriteLine($"PASS: {scenario}"); return; }
    throw new InvalidOperationException($"Scenario failed: {scenario}");
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string scenario) where TException : Exception
{
    try { await action(); }
    catch (TException) { Console.WriteLine($"PASS: {scenario}"); return; }
    throw new InvalidOperationException($"Scenario failed: {scenario}");
}

sealed class RecordingAfWalIdLookup(WalletId? result) : IAfWalIdRecipientLookup
{
    public int Calls { get; private set; }
    public Currency? LastCurrency { get; private set; }

    public Task<WalletId?> ResolveAsync(string afWalId, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCurrency = currency;
        return Task.FromResult(result);
    }
}

sealed class RecordingQrLookup(WalletId? result) : IQrRecipientLookup
{
    public int Calls { get; private set; }

    public Task<WalletId?> ResolveAsync(string qrToken, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(result);
    }
}

sealed class RecordingP2PTransferPort : IP2PTransferPort
{
    public int Calls { get; private set; }
    public Guid LastSourceWalletId { get; private set; }
    public Guid LastTargetWalletId { get; private set; }
    public long LastAmountMinor { get; private set; }
    public Guid LastCorrelationId { get; private set; }
    public DateTimeOffset LastRequestedAtUtc { get; private set; }

    public Task<P2PTransferReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastSourceWalletId = sourceWalletId;
        LastTargetWalletId = targetWalletId;
        LastAmountMinor = amountMinor;
        LastCorrelationId = correlationId;
        LastRequestedAtUtc = requestedAtUtc;

        return Task.FromResult(new P2PTransferReceipt(
            Guid.NewGuid(),
            sourceWalletId,
            targetWalletId,
            "EUR",
            amountMinor,
            correlationId,
            requestedAtUtc));
    }
}
