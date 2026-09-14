using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var userId = Guid.NewGuid();
var port = new RecordingMailboxQueryPort();
var service = new PaymentRequestMailboxQueryService(port);

var inbox = await service.ListAsync(new PaymentRequestListQuery(
    userId,
    PaymentRequestMailbox.Inbox,
    PaymentRequestStatus.Pending,
    25,
    "opaque-cursor"));

Assert(port.Calls == 1, "Query port must be called once.");
Assert(port.LastQuery is not null, "Query must be forwarded.");
Assert(port.LastQuery!.UserId == userId, "User id must be forwarded.");
Assert(port.LastQuery.Mailbox == PaymentRequestMailbox.Inbox, "Inbox mailbox must be forwarded.");
Assert(port.LastQuery.Status == PaymentRequestStatus.Pending, "Status filter must be forwarded.");
Assert(port.LastQuery.PageSize == 25, "Page size must be forwarded.");
Assert(port.LastQuery.Cursor == "opaque-cursor", "Cursor must be forwarded unchanged.");
Assert(inbox.NextCursor == "next-cursor", "Next cursor must be preserved.");

await service.ListAsync(new PaymentRequestListQuery(userId, PaymentRequestMailbox.Outbox));
Assert(port.LastQuery?.Mailbox == PaymentRequestMailbox.Outbox, "Outbox mailbox must be supported.");

AssertThrows<ArgumentException>(
    () => service.ListAsync(new PaymentRequestListQuery(Guid.Empty, PaymentRequestMailbox.Inbox)).GetAwaiter().GetResult(),
    "Empty user id must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => service.ListAsync(new PaymentRequestListQuery(userId, (PaymentRequestMailbox)999)).GetAwaiter().GetResult(),
    "Unknown mailbox must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => service.ListAsync(new PaymentRequestListQuery(userId, PaymentRequestMailbox.Inbox, PageSize: 0)).GetAwaiter().GetResult(),
    "Zero page size must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => service.ListAsync(new PaymentRequestListQuery(userId, PaymentRequestMailbox.Inbox, PageSize: 101)).GetAwaiter().GetResult(),
    "Page size above 100 must be rejected.");
AssertThrows<ArgumentException>(
    () => service.ListAsync(new PaymentRequestListQuery(userId, PaymentRequestMailbox.Inbox, Cursor: "   ")).GetAwaiter().GetResult(),
    "Blank cursor must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ListAsync(new PaymentRequestListQuery(userId, PaymentRequestMailbox.Inbox), cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-LIST-1 mailbox query foundation scenarios: PASS");

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

sealed class RecordingMailboxQueryPort : IPaymentRequestMailboxQueryPort
{
    public int Calls { get; private set; }
    public PaymentRequestListQuery? LastQuery { get; private set; }

    public Task<PaymentRequestListPage> ListAsync(
        PaymentRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastQuery = query;
        return Task.FromResult(new PaymentRequestListPage(Array.Empty<PaymentRequestSnapshot>(), "next-cursor"));
    }
}
