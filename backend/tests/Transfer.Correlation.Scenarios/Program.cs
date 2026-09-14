using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

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

var correlationId = Guid.NewGuid();
var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero);
var receipt = TransferReceiptReadModel.Create(
    TransferId.New(),
    JournalEntryId.New(),
    sourceWalletId,
    targetWalletId,
    " eur ",
    12_500,
    correlationId,
    createdAt);

Assert(receipt.CurrencyCode == "EUR", "Receipt currency must be normalized.");
Assert(receipt.AmountMinor == 12_500, "Receipt amount must be preserved.");
Assert(receipt.SourceWalletId == sourceWalletId, "Source wallet must be preserved.");
Assert(receipt.TargetWalletId == targetWalletId, "Target wallet must be preserved.");
Assert(receipt.CorrelationId == correlationId, "Correlation id must be preserved.");

var reader = new FakeReceiptReader(receipt);
var service = new TransferCorrelationLookupService(reader);
var found = await service.FindAsync(correlationId);
Assert(found.Status == TransferCorrelationLookupStatus.Found, "Existing receipt must return Found.");
Assert(found.Receipt == receipt, "Lookup must return the reader receipt unchanged.");
Assert(reader.LastCorrelationId == correlationId, "Lookup must forward correlation id unchanged.");

var missing = await new TransferCorrelationLookupService(new FakeReceiptReader(null)).FindAsync(Guid.NewGuid());
Assert(missing.Status == TransferCorrelationLookupStatus.NotFound, "Missing receipt must return NotFound.");
Assert(missing.Receipt is null, "NotFound must not expose a receipt.");

var requestedCorrelation = Guid.NewGuid();
var mismatched = TransferReceiptReadModel.Create(
    TransferId.New(),
    JournalEntryId.New(),
    Guid.NewGuid(),
    Guid.NewGuid(),
    "XAF",
    1_000,
    Guid.NewGuid(),
    createdAt);
try
{
    await new TransferCorrelationLookupService(new FakeReceiptReader(mismatched)).FindAsync(requestedCorrelation);
    throw new InvalidOperationException("Expected mismatched correlation rejection.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("does not match", StringComparison.Ordinal)) { }

AssertThrows<ArgumentException>(
    () => service.FindAsync(Guid.Empty).GetAwaiter().GetResult(),
    "Empty lookup correlation must be rejected.");
AssertThrows<ArgumentException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), Guid.Empty, targetWalletId, "EUR", 1, correlationId, createdAt),
    "Empty source wallet must be rejected.");
AssertThrows<ArgumentException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, Guid.Empty, "EUR", 1, correlationId, createdAt),
    "Empty target wallet must be rejected.");
AssertThrows<InvalidOperationException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, sourceWalletId, "EUR", 1, correlationId, createdAt),
    "Self transfer receipt must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, targetWalletId, "EUR", 0, correlationId, createdAt),
    "Non-positive amount must be rejected.");
AssertThrows<ArgumentException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, targetWalletId, "EURO", 1, correlationId, createdAt),
    "Invalid currency must be rejected.");
AssertThrows<ArgumentException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, targetWalletId, "EUR", 1, Guid.Empty, createdAt),
    "Empty receipt correlation must be rejected.");
AssertThrows<ArgumentException>(
    () => TransferReceiptReadModel.Create(TransferId.New(), JournalEntryId.New(), sourceWalletId, targetWalletId, "EUR", 1, correlationId, createdAt.ToOffset(TimeSpan.FromHours(2))),
    "Non-UTC receipt timestamp must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.FindAsync(correlationId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-TRANSFER-CORRELATION-1 receipt read-model foundation scenarios: PASS");

sealed class FakeReceiptReader(TransferReceiptReadModel? receipt) : ITransferReceiptReader
{
    public Guid? LastCorrelationId { get; private set; }

    public Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastCorrelationId = correlationId;
        return Task.FromResult(receipt);
    }
}
