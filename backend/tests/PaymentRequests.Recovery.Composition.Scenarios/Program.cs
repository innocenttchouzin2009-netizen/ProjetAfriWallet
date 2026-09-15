using AfriWallet.Ledger.Domain;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.PaymentRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-request-recovery-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={databasePath}";

try
{
    var receiptReader = new RecordingTransferReceiptReader();
    var paymentPort = new RecordingPaymentPort();

    var services = new ServiceCollection();
    services.AddSingleton<ITransferReceiptReader>(receiptReader);
    services.AddScoped<TransferCorrelationLookupService>();
    services.AddPaymentRequests(connectionString);
    services.AddSingleton<IPaymentRequestPaymentPort>(paymentPort);

    Assert(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPaymentRequestReconciliationPort) &&
            descriptor.ImplementationType == typeof(TransferCorrelationPaymentRequestReconciliationPort)),
        "Host composition must wire IPaymentRequestReconciliationPort to TransferCorrelationPaymentRequestReconciliationPort.");
    Assert(services.Any(descriptor => descriptor.ServiceType == typeof(PaymentRequestRecoveryService)),
        "Host composition must register PaymentRequestRecoveryService.");

    await using var provider = services.BuildServiceProvider();

    var requesterWalletId = WalletId.From(Guid.NewGuid());
    var payerWalletId = WalletId.From(Guid.NewGuid());
    var createdAtUtc = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
    var acceptedAtUtc = createdAtUtc.AddMinutes(2);
    var amountMinor = 12_500L;

    PaymentRequest request;
    await using (var setupScope = provider.CreateAsyncScope())
    {
        var db = setupScope.ServiceProvider.GetRequiredService<PaymentRequestDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        request = PaymentRequest.Create(
            requesterWalletId,
            RecipientReference.FromAfWalId("payer.recovery.e2e"),
            Currency.Create("XAF"),
            amountMinor,
            Guid.NewGuid(),
            createdAtUtc,
            createdAtUtc.AddHours(2));
        request.Accept(payerWalletId, acceptedAtUtc);

        var repository = setupScope.ServiceProvider.GetRequiredService<IPaymentRequestRepository>();
        await repository.AddAsync(request);
    }

    var transferId = TransferId.New();
    var transferCreatedAtUtc = acceptedAtUtc.AddMinutes(1);
    receiptReader.Receipt = TransferReceiptReadModel.Create(
        transferId,
        JournalEntryId.New(),
        payerWalletId.Value,
        requesterWalletId.Value,
        "XAF",
        amountMinor,
        request.Id.Value,
        transferCreatedAtUtc);

    await using (var recoveryScope = provider.CreateAsyncScope())
    {
        var recoveryService = recoveryScope.ServiceProvider.GetRequiredService<PaymentRequestRecoveryService>();
        var result = await recoveryService.ReconcileAsync(request.Id);

        Assert(result.Status == PaymentRequestRecoveryStatus.Reconciled,
            "Accepted request with matching existing transfer must reconcile.");
        Assert(result.Request?.Status == PaymentRequestStatus.Paid,
            "Reconciled request must become Paid.");
        Assert(result.Request?.TransferId == transferId.Value,
            "Reconciled request must reuse the existing transfer id.");
    }

    await using (var verifyScope = provider.CreateAsyncScope())
    {
        var repository = verifyScope.ServiceProvider.GetRequiredService<IPaymentRequestRepository>();
        var persisted = await repository.GetAsync(request.Id);

        Assert(persisted is not null, "Reconciled payment request must remain persisted.");
        Assert(persisted!.Status == PaymentRequestStatus.Paid,
            "Persisted request must be Paid after reconciliation.");
        Assert(persisted.TransferId == transferId.Value,
            "Persisted request must reference the pre-existing transfer.");
    }

    Assert(receiptReader.Calls == 1,
        "Recovery must perform exactly one transfer correlation lookup.");
    Assert(receiptReader.LastCorrelationId == request.Id.Value,
        "Recovery must query the existing transfer by payment request id correlation.");
    Assert(paymentPort.Calls == 0,
        "Recovery must not execute a second payment transfer.");

    await using (var idempotentScope = provider.CreateAsyncScope())
    {
        var recoveryService = idempotentScope.ServiceProvider.GetRequiredService<PaymentRequestRecoveryService>();
        var repeated = await recoveryService.ReconcileAsync(request.Id);
        Assert(repeated.Status == PaymentRequestRecoveryStatus.AlreadyPaid,
            "Repeated recovery must be idempotent for an already-paid request.");
    }

    Assert(receiptReader.Calls == 1,
        "Already-paid recovery must not query transfer evidence again.");
    Assert(paymentPort.Calls == 0,
        "Repeated recovery must still not create a transfer.");

    Console.WriteLine("AFW-BE-REQUEST-RECOVERY-1 composition and end-to-end reconciliation scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath))
    {
        File.Delete(databasePath);
    }
}

sealed class RecordingTransferReceiptReader : ITransferReceiptReader
{
    public TransferReceiptReadModel? Receipt { get; set; }
    public int Calls { get; private set; }
    public Guid? LastCorrelationId { get; private set; }

    public Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastCorrelationId = correlationId;
        return Task.FromResult(
            Receipt is not null && Receipt.CorrelationId == correlationId
                ? Receipt
                : null);
    }
}

sealed class RecordingPaymentPort : IPaymentRequestPaymentPort
{
    public int Calls { get; private set; }

    public Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        throw new InvalidOperationException("Recovery must never execute a new payment transfer.");
    }
}
