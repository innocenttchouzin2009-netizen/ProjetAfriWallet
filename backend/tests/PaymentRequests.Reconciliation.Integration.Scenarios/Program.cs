using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Transfer.Infrastructure;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

await RunMatchingEndToEndAsync();
await RunFailClosedAsync(MismatchKind.SourceWallet, "source wallet mismatch");
await RunFailClosedAsync(MismatchKind.TargetWallet, "target wallet mismatch");
await RunFailClosedAsync(MismatchKind.Amount, "amount mismatch");
await RunFailClosedAsync(MismatchKind.Currency, "currency mismatch");
await RunFailClosedAsync(MismatchKind.Timestamp, "receipt timestamp before acceptance");
await RunFailClosedAsync(MismatchKind.BusinessReference, "invalid transfer business reference");
await RunMissingCorrelationAsync();

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-1 end-to-end integrity scenarios: PASS");

static async Task RunMatchingEndToEndAsync()
{
    await using var harness = await ReconciliationHarness.CreateAsync(MismatchKind.None);
    var rowsBefore = await harness.LedgerRowCountAsync();

    var result = await harness.Service.ReconcileAsync(harness.Request.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.Reconciled,
        "Matching real ledger receipt must reconcile Accepted -> Paid.");

    var persisted = await harness.ReloadRequestAsync();
    Assert(persisted?.Status == PaymentRequestStatus.Paid,
        "SQLite payment request must persist Paid after reconciliation.");
    Assert(persisted?.TransferId == harness.TransferId,
        "SQLite payment request must persist the ledger transfer id.");
    Assert(harness.JournalRepository.GetByCorrelationIdCalls == 1,
        "First reconciliation must read the ledger exactly once by correlation id.");
    Assert(harness.JournalRepository.AddCalls == 0,
        "Reconciliation must never write a ledger journal.");
    Assert(await harness.LedgerRowCountAsync() == rowsBefore,
        "Ledger journal row count must remain unchanged after reconciliation.");

    var replay = await harness.Service.ReconcileAsync(harness.Request.Id);
    Assert(replay.Status == PaymentRequestReconciliationStatus.AlreadyPaid,
        "Paid reconciliation replay must be idempotent.");
    Assert(replay.Request?.TransferId == harness.TransferId,
        "Paid replay must preserve the original transfer id.");
    Assert(harness.JournalRepository.GetByCorrelationIdCalls == 1,
        "Paid replay must not read transfer history again.");
    Assert(harness.JournalRepository.AddCalls == 0,
        "Paid replay must never write Ledger.");
    Assert(await harness.LedgerRowCountAsync() == rowsBefore,
        "Paid replay must leave Ledger completely unchanged.");
}

static async Task RunFailClosedAsync(MismatchKind mismatch, string scenario)
{
    await using var harness = await ReconciliationHarness.CreateAsync(mismatch);
    var rowsBefore = await harness.LedgerRowCountAsync();

    await AssertThrowsAsync<InvalidOperationException>(
        () => harness.Service.ReconcileAsync(harness.Request.Id),
        $"{scenario} must fail closed.");

    var persisted = await harness.ReloadRequestAsync();
    Assert(persisted?.Status == PaymentRequestStatus.Accepted,
        $"{scenario} must leave SQLite request Accepted.");
    Assert(persisted?.TransferId is null,
        $"{scenario} must not persist a transfer id.");
    Assert(harness.JournalRepository.AddCalls == 0,
        $"{scenario} must not create a ledger journal.");
    Assert(await harness.LedgerRowCountAsync() == rowsBefore,
        $"{scenario} must leave Ledger row count unchanged.");
}

static async Task RunMissingCorrelationAsync()
{
    await using var harness = await ReconciliationHarness.CreateAsync(MismatchKind.Correlation);
    var rowsBefore = await harness.LedgerRowCountAsync();

    var result = await harness.Service.ReconcileAsync(harness.Request.Id);

    Assert(result.Status == PaymentRequestReconciliationStatus.TransferNotFound,
        "A ledger journal under another correlation must not prove payment.");
    var persisted = await harness.ReloadRequestAsync();
    Assert(persisted?.Status == PaymentRequestStatus.Accepted,
        "Missing matching correlation must leave request Accepted.");
    Assert(harness.JournalRepository.AddCalls == 0,
        "Missing matching correlation must not write Ledger.");
    Assert(await harness.LedgerRowCountAsync() == rowsBefore,
        "Missing matching correlation must leave Ledger unchanged.");
}

enum MismatchKind
{
    None,
    SourceWallet,
    TargetWallet,
    Amount,
    Currency,
    Timestamp,
    BusinessReference,
    Correlation
}

sealed class ReconciliationHarness : IAsyncDisposable
{
    private readonly SqliteConnection paymentConnection;
    private readonly SqliteConnection ledgerConnection;
    private readonly PaymentRequestDbContext paymentDb;
    private readonly LedgerDbContext ledgerDb;
    private readonly EfPaymentRequestRepository paymentRepository;

    private ReconciliationHarness(
        SqliteConnection paymentConnection,
        SqliteConnection ledgerConnection,
        PaymentRequestDbContext paymentDb,
        LedgerDbContext ledgerDb,
        EfPaymentRequestRepository paymentRepository,
        GuardedJournalRepository journalRepository,
        PaymentRequestReconciliationService service,
        PaymentRequest request,
        Guid transferId)
    {
        this.paymentConnection = paymentConnection;
        this.ledgerConnection = ledgerConnection;
        this.paymentDb = paymentDb;
        this.ledgerDb = ledgerDb;
        this.paymentRepository = paymentRepository;
        JournalRepository = journalRepository;
        Service = service;
        Request = request;
        TransferId = transferId;
    }

    public GuardedJournalRepository JournalRepository { get; }
    public PaymentRequestReconciliationService Service { get; }
    public PaymentRequest Request { get; }
    public Guid TransferId { get; }

    public static async Task<ReconciliationHarness> CreateAsync(MismatchKind mismatch)
    {
        var paymentConnection = new SqliteConnection("Data Source=:memory:");
        var ledgerConnection = new SqliteConnection("Data Source=:memory:");
        await paymentConnection.OpenAsync();
        await ledgerConnection.OpenAsync();

        var paymentOptions = new DbContextOptionsBuilder<PaymentRequestDbContext>()
            .UseSqlite(paymentConnection)
            .Options;
        var ledgerOptions = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(ledgerConnection)
            .Options;

        var paymentDb = new PaymentRequestDbContext(paymentOptions);
        var ledgerDb = new LedgerDbContext(ledgerOptions);
        await paymentDb.Database.EnsureCreatedAsync();
        await ledgerDb.Database.EnsureCreatedAsync();

        var paymentRepository = new EfPaymentRequestRepository(paymentDb);
        var ledgerRepository = new EfJournalRepository(ledgerDb);

        var requesterWallet = WalletId.From(Guid.NewGuid());
        var payerWallet = WalletId.From(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 14, 16, 0, 0, TimeSpan.Zero);
        var acceptedAt = createdAt.AddMinutes(1);
        var postedAt = mismatch == MismatchKind.Timestamp ? createdAt : acceptedAt.AddMinutes(1);

        var request = PaymentRequest.Create(
            requesterWallet,
            RecipientReference.FromAfWalId("payer.reconcile"),
            Currency.Create("XAF"),
            2_500,
            Guid.NewGuid(),
            createdAt,
            createdAt.AddHours(1));
        request.Accept(payerWallet, acceptedAt);
        await paymentRepository.AddAsync(request);

        var transferId = Guid.NewGuid();
        var debitAccount = new AccountId(Guid.NewGuid());
        var creditAccount = new AccountId(Guid.NewGuid());
        var journalAmount = mismatch == MismatchKind.Amount ? 2_501 : request.AmountMinor;
        var journalCurrency = mismatch == MismatchKind.Currency ? "XOF" : request.Currency.Code;
        var journalCorrelation = mismatch == MismatchKind.Correlation ? Guid.NewGuid() : request.Id.Value;
        var businessReference = mismatch == MismatchKind.BusinessReference
            ? "NOT-A-TRANSFER"
            : $"TRF-{transferId}";

        var journal = JournalEntry.Create(
            JournalEntryId.New(),
            journalCurrency,
            businessReference,
            journalCorrelation,
            postedAt,
            [
                new LedgerLine(debitAccount, LedgerSide.Debit, journalAmount, "payment-request"),
                new LedgerLine(creditAccount, LedgerSide.Credit, journalAmount, "payment-request")
            ]);
        await ledgerRepository.AddAsync(journal);

        var mappedSourceWallet = mismatch == MismatchKind.SourceWallet
            ? Guid.NewGuid()
            : payerWallet.Value;
        var mappedTargetWallet = mismatch == MismatchKind.TargetWallet
            ? Guid.NewGuid()
            : requesterWallet.Value;
        var mappings = new Dictionary<Guid, AccountId>
        {
            [mappedSourceWallet] = debitAccount,
            [mappedTargetWallet] = creditAccount
        };

        var guardedJournalRepository = new GuardedJournalRepository(ledgerRepository);
        var ledgerReceiptReader = new LedgerBackedTransferReceiptReader(guardedJournalRepository, mappings);
        var paymentReceiptReader = new TransferCorrelationPaymentReceiptReader(ledgerReceiptReader);
        var service = new PaymentRequestReconciliationService(paymentRepository, paymentReceiptReader);

        return new ReconciliationHarness(
            paymentConnection,
            ledgerConnection,
            paymentDb,
            ledgerDb,
            paymentRepository,
            guardedJournalRepository,
            service,
            request,
            transferId);
    }

    public Task<int> LedgerRowCountAsync() => ledgerDb.JournalEntries.AsNoTracking().CountAsync();

    public async Task<PaymentRequest?> ReloadRequestAsync()
    {
        paymentDb.ChangeTracker.Clear();
        return await paymentRepository.GetAsync(Request.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await paymentDb.DisposeAsync();
        await ledgerDb.DisposeAsync();
        await paymentConnection.DisposeAsync();
        await ledgerConnection.DisposeAsync();
    }
}

sealed class GuardedJournalRepository(IJournalRepository inner) : IJournalRepository
{
    public int AddCalls { get; private set; }
    public int GetByCorrelationIdCalls { get; private set; }

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        inner.ExistsByCorrelationIdAsync(correlationId, cancellationToken);

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        AddCalls++;
        throw new InvalidOperationException("Reconciliation is read-only against Ledger.");
    }

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default) =>
        inner.GetAsync(journalEntryId, cancellationToken);

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        GetByCorrelationIdCalls++;
        return inner.GetByCorrelationIdAsync(correlationId, cancellationToken);
    }
}
