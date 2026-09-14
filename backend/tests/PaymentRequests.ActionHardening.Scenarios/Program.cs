using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var createdAt = new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero);
var actionAt = createdAt.AddMinutes(5);
var requesterOwner = Guid.NewGuid();
var payerOwner = Guid.NewGuid();
var stranger = Guid.NewGuid();
var requesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var changedWallet = WalletId.From(Guid.NewGuid());
var currency = Currency.Create("XAF");
var payerReference = RecipientReference.FromAfWalId("payer.one");

PaymentRequest NewRequest() => PaymentRequest.Create(
    requesterWallet,
    payerReference,
    currency,
    2_500,
    Guid.NewGuid(),
    createdAt,
    createdAt.AddHours(1));

var ownership = new FixedOwnershipReader(new Dictionary<Guid, Guid>
{
    [requesterWallet.Value] = requesterOwner,
    [payerWallet.Value] = payerOwner,
    [changedWallet.Value] = stranger
});

// Paid replay must be bound to persisted payer wallet and must not re-resolve public identity.
{
    var request = NewRequest();
    request.Accept(payerWallet, actionAt);
    request.MarkPaid(Guid.NewGuid(), actionAt.AddMinutes(1));
    var repository = new InMemoryRepository(request);
    var resolver = new RecordingResolver(null);
    var payment = new RecordingPaymentPort();
    var service = new PaymentRequestActionService(repository, resolver, ownership, payment);

    var replay = await service.AcceptAndPayAsync(request.Id, payerOwner, actionAt.AddMinutes(2));
    Assert(replay.Status == PaymentRequestActionStatus.Success, "Paid replay must succeed for persisted payer owner.");
    Assert(replay.Request?.Status == PaymentRequestStatus.Paid, "Paid replay must remain Paid.");
    Assert(resolver.Calls == 0, "Paid replay must not re-resolve AfWal ID or QR.");
    Assert(payment.Calls == 0, "Paid replay must not execute a second payment.");

    var denied = await service.AcceptAndPayAsync(request.Id, stranger, actionAt.AddMinutes(3));
    Assert(denied.Status == PaymentRequestActionStatus.ActorNotAllowed, "Paid replay must remain payer-owner protected.");
    Assert(resolver.Calls == 0, "Unauthorized Paid replay must still avoid public identity re-resolution.");
    Assert(payment.Calls == 0, "Unauthorized Paid replay must not execute payment.");
}

// Accepted retry must use persisted payer wallet even if the public identity now resolves elsewhere.
{
    var request = NewRequest();
    request.Accept(payerWallet, actionAt);
    var repository = new InMemoryRepository(request);
    var resolver = new RecordingResolver(changedWallet);
    var payment = new RecordingPaymentPort();
    var service = new PaymentRequestActionService(repository, resolver, ownership, payment);

    var paid = await service.AcceptAndPayAsync(request.Id, payerOwner, actionAt.AddMinutes(1));
    Assert(paid.Status == PaymentRequestActionStatus.Success, "Accepted retry must succeed for bound payer owner.");
    Assert(paid.Request?.Status == PaymentRequestStatus.Paid, "Accepted retry must become Paid.");
    Assert(resolver.Calls == 0, "Accepted retry must not re-resolve changed public identity.");
    Assert(payment.Calls == 1, "Accepted retry must execute exactly one payment.");
    Assert(payment.SourceWalletId == payerWallet.Value, "Accepted retry must pay from persisted payer wallet.");
    Assert(payment.TargetWalletId == requesterWallet.Value, "Accepted retry must pay requester wallet.");

    var stored = await repository.GetAsync(request.Id);
    Assert(stored?.AcceptedPayerWalletId == payerWallet, "Persisted payer binding must remain unchanged.");
}

// Pending acceptance still requires fresh resolution and binds that wallet before money execution.
{
    var request = NewRequest();
    var repository = new InMemoryRepository(request);
    var resolver = new RecordingResolver(payerWallet);
    var payment = new RecordingPaymentPort();
    var service = new PaymentRequestActionService(repository, resolver, ownership, payment);

    var paid = await service.AcceptAndPayAsync(request.Id, payerOwner, actionAt);
    Assert(paid.Status == PaymentRequestActionStatus.Success, "Pending request must still resolve and pay.");
    Assert(resolver.Calls == 1, "Pending request must resolve payer exactly once.");
    Assert(repository.UpdateCalls == 2, "Pending request must persist Accepted then Paid.");
    Assert(payment.SourceWalletId == payerWallet.Value, "Pending request must pay from resolved payer wallet.");
}

Console.WriteLine("AFW-BE-REQUEST-ACTIONS-1 payer binding hardening scenarios: PASS");

sealed class RecordingResolver(WalletId? result) : IPaymentRequestRecipientResolver
{
    public int Calls { get; private set; }

    public Task<WalletId?> ResolveAsync(
        RecipientReference reference,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(result);
    }
}

sealed class FixedOwnershipReader(IReadOnlyDictionary<Guid, Guid> owners) : IPaymentRequestWalletOwnershipReader
{
    public Task<bool> IsOwnedByAsync(WalletId walletId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(owners.TryGetValue(walletId.Value, out var actual) && actual == ownerId);
    }
}

sealed class RecordingPaymentPort : IPaymentRequestPaymentPort
{
    public int Calls { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid TargetWalletId { get; private set; }

    public Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        return Task.FromResult(new PaymentRequestPaymentReceipt(
            Guid.NewGuid(),
            sourceWalletId,
            targetWalletId,
            amountMinor,
            correlationId,
            requestedAtUtc));
    }
}

sealed class InMemoryRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = requests.ToDictionary(x => x.Id.Value, x => x);
    private readonly Dictionary<Guid, PaymentRequest> byCorrelation = requests.ToDictionary(x => x.CorrelationId, x => x);
    public int UpdateCalls { get; private set; }

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

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = request;
        byCorrelation[request.CorrelationId] = request;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}
