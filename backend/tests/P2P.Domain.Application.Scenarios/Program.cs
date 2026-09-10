using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.Wallet.Domain;

var eur = Currency.Create("eur");
var expectedWalletId = WalletId.New();
var afWalLookup = new RecordingAfWalIdLookup(expectedWalletId);
var qrLookup = new RecordingQrLookup(expectedWalletId);
var service = new RecipientResolutionService(afWalLookup, qrLookup);

var afWalReference = RecipientReference.FromAfWalId("  leaty.237  ");
Assert(afWalReference.Kind == RecipientReferenceKind.AfWalId, "AfWal ID kind");
Assert(afWalReference.Value == "leaty.237", "AfWal ID trim normalization");

var afWalResult = await service.ResolveAsync(new ResolveRecipientRequest(afWalReference, eur));
Assert(afWalResult.Status == RecipientResolutionStatus.Success, "AfWal ID resolution succeeds");
Assert(afWalResult.Recipient?.WalletId == expectedWalletId, "AfWal ID resolves wallet");
Assert(afWalLookup.LastCurrency?.Code == "EUR", "currency forwarded to AfWal lookup");
Assert(qrLookup.Calls == 0, "QR lookup not used for AfWal ID");

var qrReference = RecipientReference.FromQrToken("opaque-recipient-token");
var qrResult = await service.ResolveAsync(new ResolveRecipientRequest(qrReference, eur));
Assert(qrResult.Status == RecipientResolutionStatus.Success, "QR resolution succeeds");
Assert(qrLookup.Calls == 1, "QR lookup used once");
Assert(afWalLookup.Calls == 1, "AfWal lookup not reused for QR");

var missingService = new RecipientResolutionService(new RecordingAfWalIdLookup(null), new RecordingQrLookup(null));
var missing = await missingService.ResolveAsync(new ResolveRecipientRequest(RecipientReference.FromAfWalId("unknown"), eur));
Assert(missing.Status == RecipientResolutionStatus.NotFound && missing.Recipient is null, "unknown recipient is not found");

AssertThrows<ArgumentException>(() => RecipientReference.FromAfWalId("   "), "blank AfWal ID rejected");
AssertThrows<ArgumentException>(() => RecipientReference.FromAfWalId("bad id"), "AfWal ID whitespace rejected");
AssertThrows<ArgumentException>(() => RecipientReference.FromQrToken(" token "), "QR surrounding whitespace rejected");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(() => service.ResolveAsync(
    new ResolveRecipientRequest(RecipientReference.FromAfWalId("cancelled"), eur), cts.Token),
    "cancellation propagated before lookup");

Console.WriteLine("AFW-BE-P2P-1 recipient resolution domain/application scenarios: PASS");

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
