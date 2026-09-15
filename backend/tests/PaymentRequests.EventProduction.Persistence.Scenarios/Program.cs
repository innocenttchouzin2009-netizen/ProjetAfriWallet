using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-request-event-production-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    await using var db = new PaymentRequestDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var repository = new EfPaymentRequestRepository(db);
    var mutationStore = new EfPaymentRequestLifecycleMutationStore(db);
    var payerWallet = WalletId.From(Guid.NewGuid());
    var requesterWallet = WalletId.From(Guid.NewGuid());
    var ownerId = Guid.NewGuid();
    var createdAt = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.Zero);
    var secretAfWalId = "payer.atomic.secret";

    var service = new PaymentRequestApplicationService(
        repository,
        new FixedResolver(payerWallet),
        mutationStore);

    var create = await service.CreateAsync(new CreatePaymentRequestCommand(
        requesterWallet,
        RecipientReference.FromAfWalId(secretAfWalId),
        Currency.Create("XAF"),
        5_000,
        Guid.NewGuid(),
        createdAt,
        createdAt.AddHours(2)));

    Assert(create.Status == CreatePaymentRequestStatus.Created && create.Request is not null, "Request must be created.");
    Assert(await db.PaymentRequests.CountAsync() == 1, "Request row must be persisted.");
    Assert(await db.PaymentRequestEventOutbox.CountAsync() == 1, "Created event must be persisted atomically.");
    var createdOutbox = await db.PaymentRequestEventOutbox.SingleAsync();
    Assert(createdOutbox.EventType == "payment-request.created", "Created event type mismatch.");
    Assert(!createdOutbox.PayloadJson.Contains(secretAfWalId, StringComparison.Ordinal), "Outbox payload must not expose AfWal ID.");

    var actionService = new PaymentRequestActionService(
        repository,
        new FixedResolver(payerWallet),
        new FixedOwnershipReader(payerWallet, ownerId),
        new SuccessfulPaymentPort(createdAt.AddMinutes(2)),
        mutationStore);

    var paid = await actionService.AcceptAndPayAsync(
        create.Request!.Id,
        ownerId,
        createdAt.AddMinutes(1));

    Assert(paid.Status == PaymentRequestActionStatus.Success && paid.Request?.Status == PaymentRequestStatus.Paid, "Accept and pay must succeed.");
    var eventTypes = await db.PaymentRequestEventOutbox
        .OrderBy(x => x.OccurredAtUtc)
        .Select(x => x.EventType)
        .ToListAsync();
    Assert(eventTypes.SequenceEqual(new[] { "payment-request.created", "payment-request.accepted", "payment-request.paid" }),
        "Created, accepted and paid events must be persisted.");

    var rollbackRequest = PaymentRequest.Create(
        WalletId.From(Guid.NewGuid()),
        RecipientReference.FromAfWalId("payer.rollback"),
        Currency.Create("XAF"),
        1_000,
        Guid.NewGuid(),
        createdAt,
        createdAt.AddHours(1));
    var rollbackCreated = PaymentRequestLifecycleEventFactory.Create(
        rollbackRequest,
        PaymentRequestLifecycleEventKind.Created,
        rollbackRequest.CreatedAtUtc);
    await mutationStore.AddAsync(rollbackRequest, rollbackCreated);

    rollbackRequest.Accept(WalletId.From(Guid.NewGuid()), createdAt.AddMinutes(3));
    var duplicateEvent = PaymentRequestLifecycleEventFactory.Create(
        rollbackRequest,
        PaymentRequestLifecycleEventKind.Accepted,
        rollbackRequest.UpdatedAtUtc) with { EventId = rollbackCreated.EventId };

    try
    {
        await mutationStore.UpdateAsync(rollbackRequest, duplicateEvent);
        throw new InvalidOperationException("Expected atomic outbox conflict.");
    }
    catch (InvalidOperationException exception) when (exception.Message.Contains("atomically", StringComparison.Ordinal)) { }

    var persistedAfterFailure = await repository.GetAsync(rollbackRequest.Id);
    Assert(persistedAfterFailure?.Status == PaymentRequestStatus.Pending,
        "Request state must roll back when outbox insert fails.");
    Assert(await db.PaymentRequestEventOutbox.CountAsync(x => x.PaymentRequestId == rollbackRequest.Id.Value) == 1,
        "Failed atomic update must not add an outbox row.");

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    var cancelledRequest = PaymentRequest.Create(
        WalletId.From(Guid.NewGuid()),
        RecipientReference.FromAfWalId("payer.cancelled"),
        Currency.Create("XAF"),
        500,
        Guid.NewGuid(),
        createdAt);
    var cancelledEvent = PaymentRequestLifecycleEventFactory.Create(
        cancelledRequest,
        PaymentRequestLifecycleEventKind.Created,
        createdAt);
    try
    {
        await mutationStore.AddAsync(cancelledRequest, cancelledEvent, cts.Token);
        throw new InvalidOperationException("Expected cancellation.");
    }
    catch (OperationCanceledException) { }

    Console.WriteLine("AFW-BE-REQUEST-EVENT-PRODUCTION-1 atomic persistence scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class FixedResolver(WalletId walletId) : IPaymentRequestRecipientResolver
{
    public Task<WalletId?> ResolveAsync(RecipientReference reference, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<WalletId?>(walletId);
    }
}

sealed class FixedOwnershipReader(WalletId walletId, Guid ownerId) : IPaymentRequestWalletOwnershipReader
{
    public Task<bool> IsOwnedByAsync(WalletId candidate, Guid candidateOwnerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(candidate == walletId && candidateOwnerId == ownerId);
    }
}

sealed class SuccessfulPaymentPort(DateTimeOffset paidAtUtc) : IPaymentRequestPaymentPort
{
    public Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentRequestPaymentReceipt(
            Guid.NewGuid(),
            sourceWalletId,
            targetWalletId,
            amountMinor,
            correlationId,
            paidAtUtc));
    }
}
