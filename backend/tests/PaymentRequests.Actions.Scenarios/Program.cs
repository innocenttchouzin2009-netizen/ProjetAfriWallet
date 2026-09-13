using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var createdAt = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
var actionAt = createdAt.AddMinutes(5);
var requesterOwner = Guid.NewGuid();
var payerOwner = Guid.NewGuid();
var stranger = Guid.NewGuid();
var requesterWallet = WalletId.From(Guid.NewGuid());
var payerWallet = WalletId.From(Guid.NewGuid());
var currency = Currency.Create("XAF");
var payerReference = RecipientReference.FromAfWalId("payer.one");

PaymentRequest NewRequest(DateTimeOffset? expiresAtUtc = null) => PaymentRequest.Create(
    requesterWallet,
    payerReference,
    currency,
    2_500,
    Guid.NewGuid(),
    createdAt,
    expiresAtUtc ?? createdAt.AddHours(1));

var ownership = new FixedOwnershipReader(new Dictionary<Guid, Guid>
{
    [requesterWallet.Value] = requesterOwner,
    [payerWallet.Value] = payerOwner
});

// Decline is payer-only.
{
    var repository = new CloningRepository(NewRequest());
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    var request = await repository.SingleAsync();
    var denied = await service.DeclineAsync(request.Id, stranger, actionAt);
    Assert(denied.Status == PaymentRequestActionStatus.ActorNotAllowed, "Foreign actor must not decline.");
    Assert(repository.UpdateCalls == 0, "Denied decline must not persist.");

    var declined = await service.DeclineAsync(request.Id, payerOwner, actionAt);
    Assert(declined.Status == PaymentRequestActionStatus.Success, "Payer must be able to decline.");
    Assert(declined.Request?.Status == PaymentRequestStatus.Declined, "Decline must persist terminal status.");
    Assert(transferPort.Calls == 0, "Decline must not execute money movement.");
}

// Cancel is requester-only.
{
    var repository = new CloningRepository(NewRequest());
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    var request = await repository.SingleAsync();
    var denied = await service.CancelAsync(request.Id, payerOwner, actionAt);
    Assert(denied.Status == PaymentRequestActionStatus.ActorNotAllowed, "Payer must not cancel requester-owned request.");

    var cancelled = await service.CancelAsync(request.Id, requesterOwner, actionAt);
    Assert(cancelled.Status == PaymentRequestActionStatus.Success, "Requester must be able to cancel.");
    Assert(cancelled.Request?.Status == PaymentRequestStatus.Cancelled, "Cancel must persist terminal status.");
    Assert(transferPort.Calls == 0, "Cancel must not execute money movement.");
}

// Accept resolves the payer, persists Accepted, validates actor ownership and pays payer -> requester.
{
    var original = NewRequest();
    var repository = new CloningRepository(original);
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    var denied = await service.AcceptAndPayAsync(original.Id, stranger, actionAt);
    Assert(denied.Status == PaymentRequestActionStatus.ActorNotAllowed, "Foreign actor must not accept/pay.");
    Assert(transferPort.Calls == 0, "Unauthorized accept must not execute transfer.");

    var paid = await service.AcceptAndPayAsync(original.Id, payerOwner, actionAt);
    Assert(paid.Status == PaymentRequestActionStatus.Success, "Payer must be able to accept/pay.");
    Assert(paid.Request?.Status == PaymentRequestStatus.Paid, "Successful payment must finish as Paid.");
    Assert(paid.Request?.AcceptedPayerWalletId == payerWallet, "Accepted payer wallet must be persisted.");
    Assert(paid.Request?.TransferId == transferPort.TransferId, "Real transfer id must be persisted.");
    Assert(transferPort.Calls == 1, "Payment must execute exactly one transfer.");
    Assert(transferPort.SourceWalletId == payerWallet.Value, "Payment source must be payer wallet.");
    Assert(transferPort.TargetWalletId == requesterWallet.Value, "Payment target must be requester wallet.");
    Assert(transferPort.AmountMinor == 2_500, "Payment amount must match request.");
    Assert(transferPort.CorrelationId == original.Id.Value, "Transfer correlation must be deterministic request id.");
    Assert(repository.UpdateCalls == 2, "Accepted and Paid states must each be persisted.");

    var replay = await service.AcceptAndPayAsync(original.Id, payerOwner, actionAt.AddMinutes(1));
    Assert(replay.Status == PaymentRequestActionStatus.Success, "Paid replay must be idempotent.");
    Assert(replay.Request?.Status == PaymentRequestStatus.Paid, "Paid replay must remain Paid.");
    Assert(replay.Request?.TransferId == transferPort.TransferId, "Paid replay must preserve the original transfer id.");
    Assert(transferPort.Calls == 1, "Paid replay must not execute a second transfer.");
    Assert(repository.UpdateCalls == 2, "Paid replay must not persist an extra transition.");
}

// Transfer failure must leave a durable Accepted state for controlled retry/reconciliation.
{
    var original = NewRequest();
    var repository = new CloningRepository(original);
    var failingPort = new RecordingP2PTransferPort { Failure = new InvalidOperationException("transfer failed") };
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(failingPort));

    try
    {
        await service.AcceptAndPayAsync(original.Id, payerOwner, actionAt);
        throw new InvalidOperationException("Expected transfer failure.");
    }
    catch (InvalidOperationException ex) when (ex.Message == "transfer failed") { }

    Assert(repository.UpdateCalls == 1, "Failed transfer must persist Accepted before money execution.");
    var persisted = await repository.GetAsync(original.Id);
    Assert(persisted?.Status == PaymentRequestStatus.Accepted, "Durable state must remain Accepted after failed transfer.");
    Assert(persisted?.AcceptedPayerWalletId == payerWallet, "Durable Accepted state must preserve payer wallet.");
}

// A previously persisted Accepted request can retry payment without accepting a second time.
{
    var accepted = NewRequest();
    accepted.Accept(payerWallet, actionAt);
    var repository = new CloningRepository(accepted);
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    var paid = await service.AcceptAndPayAsync(accepted.Id, payerOwner, actionAt.AddMinutes(1));
    Assert(paid.Status == PaymentRequestActionStatus.Success, "Accepted retry must be payable.");
    Assert(paid.Request?.Status == PaymentRequestStatus.Paid, "Accepted retry must become Paid.");
    Assert(transferPort.Calls == 1, "Accepted retry must execute one transfer.");
    Assert(repository.UpdateCalls == 1, "Accepted retry must persist only the Paid transition.");
}

// An Accepted request that has reached its expiration must fail before transfer execution.
{
    var accepted = NewRequest(createdAt.AddMinutes(6));
    accepted.Accept(payerWallet, createdAt.AddMinutes(1));
    var repository = new CloningRepository(accepted);
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    try
    {
        await service.AcceptAndPayAsync(accepted.Id, payerOwner, createdAt.AddMinutes(6));
        throw new InvalidOperationException("Expected expiration failure.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)) { }

    Assert(transferPort.Calls == 0, "Expired Accepted retry must not execute transfer.");
    Assert(repository.UpdateCalls == 0, "Expired Accepted retry must not change durable state.");
}

// Missing recipient fails closed before transfer.
{
    var original = NewRequest();
    var repository = new CloningRepository(original);
    var transferPort = new RecordingP2PTransferPort();
    var service = new PaymentRequestActionService(
        repository,
        new FixedResolver(null),
        ownership,
        new P2PPaymentRequestPaymentPort(transferPort));

    var result = await service.AcceptAndPayAsync(original.Id, payerOwner, actionAt);
    Assert(result.Status == PaymentRequestActionStatus.RecipientNotFound, "Missing payer resolution must fail closed.");
    Assert(transferPort.Calls == 0, "Missing payer must not execute transfer.");
    Assert(repository.UpdateCalls == 0, "Missing payer must not update request.");
}

Console.WriteLine("AFW-BE-REQUEST-1 payment request action and P2P execution scenarios: PASS");

sealed class FixedResolver(WalletId? walletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(
        RecipientReference reference,
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(walletId);
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

sealed class RecordingP2PTransferPort : IP2PTransferPort
{
    public Guid TransferId { get; } = Guid.NewGuid();
    public int Calls { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid TargetWalletId { get; private set; }
    public long AmountMinor { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Exception? Failure { get; init; }

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
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        AmountMinor = amountMinor;
        CorrelationId = correlationId;
        if (Failure is not null) throw Failure;
        return Task.FromResult(new P2PTransferReceipt(
            TransferId,
            sourceWalletId,
            targetWalletId,
            "XAF",
            amountMinor,
            correlationId,
            requestedAtUtc));
    }
}

sealed class CloningRepository(params PaymentRequest[] requests) : IPaymentRequestRepository
{
    private readonly Dictionary<Guid, PaymentRequest> byId = requests.ToDictionary(x => x.Id.Value, Clone);
    private readonly Dictionary<Guid, Guid> byCorrelation = requests.ToDictionary(x => x.CorrelationId, x => x.Id.Value);
    public int UpdateCalls { get; private set; }

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(byId.TryGetValue(id.Value, out var value) ? Clone(value) : null);
    }

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(byCorrelation.TryGetValue(correlationId, out var id) && byId.TryGetValue(id, out var value) ? Clone(value) : null);
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.Add(request.Id.Value, Clone(request));
        byCorrelation.Add(request.CorrelationId, request.Id.Value);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId[request.Id.Value] = Clone(request);
        byCorrelation[request.CorrelationId] = request.Id.Value;
        UpdateCalls++;
        return Task.CompletedTask;
    }

    public Task<PaymentRequest> SingleAsync() => Task.FromResult(Clone(byId.Values.Single()));

    private static PaymentRequest Clone(PaymentRequest request) => PaymentRequest.Restore(
        request.Id,
        request.RequesterWalletId,
        request.PayerReference,
        request.Currency,
        request.AmountMinor,
        request.CorrelationId,
        request.CreatedAtUtc,
        request.ExpiresAtUtc,
        request.UpdatedAtUtc,
        request.Status,
        request.AcceptedPayerWalletId,
        request.AcceptedAtUtc,
        request.TransferId,
        request.ClosedAtUtc);
}
