using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var currency = Currency.Create("EUR");
var createdAt = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
var correlationId = Guid.NewGuid();
var afWalRef = RecipientReference.FromAfWalId("payer.afwal");

var afWalLookup = new FakeAfWalLookup(payer);
var qrLookup = new FakeQrLookup(null);
var p2pResolver = new RecipientResolutionService(afWalLookup, qrLookup);
var resolver = new P2PRecipientResolver(p2pResolver);
var repository = new InMemoryRepository();
var service = new PaymentRequestApplicationService(repository, resolver);

var created = await service.CreateAsync(new CreatePaymentRequestCommand(
    requester, afWalRef, currency, 5000, correlationId, createdAt, createdAt.AddHours(1)));
Assert(created.Status == CreatePaymentRequestStatus.Created, "Request must be created.");
Assert(created.Request is not null && created.Request.Status == PaymentRequestStatus.Pending, "Created request must be pending.");
Assert(afWalLookup.Calls == 1 && qrLookup.Calls == 0, "AfWal ID must use only AfWal lookup.");
Assert(repository.AddCalls == 1, "Request must be stored exactly once.");

var read = await service.GetAsync(created.Request!.Id);
Assert(read is not null && read.Id == created.Request.Id, "Created request must be readable.");

var repeated = await service.CreateAsync(new CreatePaymentRequestCommand(
    requester, afWalRef, currency, 5000, correlationId, createdAt, createdAt.AddHours(1)));
Assert(repeated.Status == CreatePaymentRequestStatus.Existing, "Same correlation must be idempotent.");
Assert(repository.AddCalls == 1, "Idempotent replay must not store twice.");

var notFoundService = new PaymentRequestApplicationService(new InMemoryRepository(), new FixedResolver(null));
var missing = await notFoundService.CreateAsync(new CreatePaymentRequestCommand(
    requester, afWalRef, currency, 1000, Guid.NewGuid(), createdAt));
Assert(missing.Status == CreatePaymentRequestStatus.RecipientNotFound, "Unknown payer must return RecipientNotFound.");

var selfService = new PaymentRequestApplicationService(new InMemoryRepository(), new FixedResolver(requester));
var self = await selfService.CreateAsync(new CreatePaymentRequestCommand(
    requester, afWalRef, currency, 1000, Guid.NewGuid(), createdAt));
Assert(self.Status == CreatePaymentRequestStatus.SelfRequestNotAllowed, "Self request must be rejected.");

var qrPayer = WalletId.From(Guid.NewGuid());
var qrResolver = new P2PRecipientResolver(new RecipientResolutionService(new FakeAfWalLookup(null), new FakeQrLookup(qrPayer)));
var qrService = new PaymentRequestApplicationService(new InMemoryRepository(), qrResolver);
var qrResult = await qrService.CreateAsync(new CreatePaymentRequestCommand(
    requester, RecipientReference.FromQrToken("opaque-token"), currency, 1000, Guid.NewGuid(), createdAt));
Assert(qrResult.Status == CreatePaymentRequestStatus.Created, "QR payer must resolve through P2P resolver.");

var conflictRepo = new InMemoryRepository();
var conflictService = new PaymentRequestApplicationService(conflictRepo, new FixedResolver(payer));
var conflictCorrelation = Guid.NewGuid();
await conflictService.CreateAsync(new CreatePaymentRequestCommand(requester, afWalRef, currency, 1000, conflictCorrelation, createdAt));
try
{
    await conflictService.CreateAsync(new CreatePaymentRequestCommand(requester, afWalRef, currency, 2000, conflictCorrelation, createdAt));
    throw new InvalidOperationException("Expected correlation conflict.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("different payment request", StringComparison.Ordinal)) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.GetAsync(created.Request.Id, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-1 application orchestration scenarios: PASS");

sealed class InMemoryRepository : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = new();
    private readonly Dictionary<Guid, PaymentRequest> byCorrelation = new();
    public int AddCalls { get; private set; }
    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        AddCalls++;
        return Task.CompletedTask;
    }
    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.TryGetValue(id.Value, out var value);
        return Task.FromResult(value);
    }
    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
    }
}

sealed class FixedResolver(WalletId? walletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(walletId);
    }
}

sealed class FakeAfWalLookup(WalletId? walletId) : IAfWalIdRecipientLookup
{
    public int Calls { get; private set; }
    public Task<WalletId?> ResolveAsync(string afWalId, Currency currency, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(walletId);
    }
}

sealed class FakeQrLookup(WalletId? walletId) : IQrRecipientLookup
{
    public int Calls { get; private set; }
    public Task<WalletId?> ResolveAsync(string qrToken, Currency currency, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(walletId);
    }
}
