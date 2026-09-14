using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var reference = RecipientReference.FromAfWalId("payer.afwal");
var currency = Currency.Create("EUR");
var createdAt = new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);
var asOf = createdAt.AddMinutes(10);

PaymentRequest Create(DateTimeOffset? expiresAtUtc) => PaymentRequest.Create(
    requester,
    reference,
    currency,
    5_000,
    Guid.NewGuid(),
    createdAt,
    expiresAtUtc);

var pendingDue = Create(createdAt.AddMinutes(5));
var acceptedDue = Create(createdAt.AddMinutes(6));
acceptedDue.Accept(payer, createdAt.AddMinutes(1));
var future = Create(createdAt.AddMinutes(20));
var noExpiry = Create(null);
var terminal = Create(createdAt.AddMinutes(5));
terminal.Decline(createdAt.AddMinutes(2));

var reader = new RecordingDueReader([pendingDue, acceptedDue, future, noExpiry, terminal]);
var repository = new RecordingRepository();
var service = new PaymentRequestExpiryService(reader, repository);

var result = await service.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf, 25));

Assert(reader.Calls == 1, "Due reader must be called once.");
Assert(reader.LastAsOfUtc == asOf, "As-of timestamp must be forwarded unchanged.");
Assert(reader.LastLimit == 25, "Batch size must be forwarded unchanged.");
Assert(result.Scanned == 5, "All returned candidates must be counted as scanned.");
Assert(result.Expired == 2, "Exactly two due active requests must expire.");
Assert(result.Skipped == 3, "Non-due and terminal requests must be skipped.");
Assert(pendingDue.Status == PaymentRequestStatus.Expired, "Pending due request must expire.");
Assert(acceptedDue.Status == PaymentRequestStatus.Expired, "Accepted due request must expire.");
Assert(pendingDue.ClosedAtUtc == asOf && acceptedDue.ClosedAtUtc == asOf, "Expiration timestamp must use the batch as-of time.");
Assert(repository.Updated.Count == 2, "Only expired requests must be persisted.");
Assert(future.Status == PaymentRequestStatus.Pending, "Future request must remain pending.");
Assert(noExpiry.Status == PaymentRequestStatus.Pending, "Request without expiry must remain pending.");
Assert(terminal.Status == PaymentRequestStatus.Declined, "Terminal request must remain unchanged.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf.ToOffset(TimeSpan.FromHours(2)), 10)),
    "Non-UTC as-of timestamp must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf, 0)),
    "Zero batch size must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf, 501)),
    "Oversized batch must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf), cts.Token),
    "Cancellation must be propagated before reading due requests.");

var nullReaderService = new PaymentRequestExpiryService(new NullDueReader(), repository);
await AssertThrowsAsync<ArgumentNullException>(
    () => nullReaderService.ExpireDueAsync(new ExpireDuePaymentRequestsCommand(asOf)),
    "Null due-reader result must fail closed.");

Console.WriteLine("AFW-BE-REQUEST-EXPIRY-1 application expiration scenarios: PASS");

sealed class RecordingDueReader(IReadOnlyList<PaymentRequest> requests) : IPaymentRequestDueReader
{
    public int Calls { get; private set; }
    public DateTimeOffset? LastAsOfUtc { get; private set; }
    public int? LastLimit { get; private set; }

    public Task<IReadOnlyList<PaymentRequest>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastAsOfUtc = asOfUtc;
        LastLimit = limit;
        return Task.FromResult(requests);
    }
}

sealed class NullDueReader : IPaymentRequestDueReader
{
    public Task<IReadOnlyList<PaymentRequest>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentRequest>>(null!);
}

sealed class RecordingRepository : IPaymentRequestRepository
{
    public List<PaymentRequest> Updated { get; } = [];

    public Task<PaymentRequest?> GetAsync(PaymentRequestId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentRequest?>(null);

    public Task<PaymentRequest?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentRequest?>(null);

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Updated.Add(request);
        return Task.CompletedTask;
    }
}
