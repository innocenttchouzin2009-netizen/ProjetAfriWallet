using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Reconciliation.Application;
using AfriWallet.PaymentRequests.Reconciliation.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
{
    try { await action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-request-reconcile-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";
var requestId = PaymentRequestId.From(Guid.NewGuid());
var transferId = Guid.NewGuid();

try
{
    await using (var first = CreateDbContext(connectionString))
    {
        await first.Database.EnsureCreatedAsync();
        var repository = new EfPaymentRequestReconciliationRecordRepository(first);
        await repository.UpsertAsync(new PaymentRequestReconciliationRecord(
            requestId,
            PaymentRequestReconciliationStatus.TransferReceiptNotFound,
            null));

        var stored = await repository.GetAsync(requestId);
        Assert(stored?.Status == PaymentRequestReconciliationStatus.TransferReceiptNotFound,
            "Initial reconciliation state must persist.");
    }

    await using (var restarted = CreateDbContext(connectionString))
    {
        var repository = new EfPaymentRequestReconciliationRecordRepository(restarted);
        var recovered = await repository.GetAsync(requestId);
        Assert(recovered is not null, "Reconciliation state must survive DbContext restart.");
        Assert(recovered!.Status == PaymentRequestReconciliationStatus.TransferReceiptNotFound,
            "Recovered reconciliation status mismatch.");

        await repository.UpsertAsync(new PaymentRequestReconciliationRecord(
            requestId,
            PaymentRequestReconciliationStatus.Reconciled,
            transferId));

        var updated = await repository.GetAsync(requestId);
        Assert(updated?.Status == PaymentRequestReconciliationStatus.Reconciled,
            "Retry must update the same durable reconciliation record.");
        Assert(updated?.TransferId == transferId, "Transfer id must survive persistence.");
        Assert(await restarted.Records.CountAsync() == 1,
            "Upsert replay must keep one row per payment request.");

        await repository.UpsertAsync(new PaymentRequestReconciliationRecord(
            requestId,
            PaymentRequestReconciliationStatus.AlreadyPaid,
            transferId));
        Assert(await restarted.Records.CountAsync() == 1,
            "Idempotent replay must not create duplicate reconciliation rows.");
    }

    await using (var uniqueness = CreateDbContext(connectionString))
    {
        var repository = new EfPaymentRequestReconciliationRecordRepository(uniqueness);
        await AssertThrowsAsync<DbUpdateException>(
            () => repository.UpsertAsync(new PaymentRequestReconciliationRecord(
                PaymentRequestId.From(Guid.NewGuid()),
                PaymentRequestReconciliationStatus.Reconciled,
                transferId)),
            "A transfer id must not reconcile two different payment requests.");
    }

    await using (var cancelled = CreateDbContext(connectionString))
    {
        var repository = new EfPaymentRequestReconciliationRecordRepository(cancelled);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await AssertThrowsAsync<OperationCanceledException>(
            () => repository.GetAsync(requestId, cts.Token),
            "Cancellation must propagate through reconciliation persistence.");
    }

    Console.WriteLine("AFW-BE-REQUEST-RECONCILE-1 persistence and durable-state scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

static PaymentRequestReconciliationDbContext CreateDbContext(string connectionString)
{
    var options = new DbContextOptionsBuilder<PaymentRequestReconciliationDbContext>()
        .UseSqlite(connectionString)
        .Options;
    return new PaymentRequestReconciliationDbContext(options);
}
