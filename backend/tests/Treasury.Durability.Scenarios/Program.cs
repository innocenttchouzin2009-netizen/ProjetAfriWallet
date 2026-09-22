using Microsoft.EntityFrameworkCore;
using Treasury.Application.Services;
using Treasury.Domain.Accounts;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;
using Treasury.Infrastructure.Persistence;
using Treasury.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-treasury-durability-{Guid.NewGuid():N}.db");
var connection = $"Data Source={dbPath}";

try
{
    Guid cashId;
    Guid clearingId;
    Guid transactionId;
    Guid reservationId;

    await using (var db = new TreasuryDbContext(
        new DbContextOptionsBuilder<TreasuryDbContext>().UseSqlite(connection).Options))
    {
        await db.Database.EnsureCreatedAsync();
        var repository = new EfTreasuryRepository(db);
        var service = new TreasuryLedgerService(repository);

        var cash = await service.CreateAccountAsync(
            "TREASURY-CASH-XAF", "Treasury Cash XAF", "XAF",
            TreasuryAccountType.Asset, CancellationToken.None);
        var clearing = await service.CreateAccountAsync(
            "TREASURY-CLEARING-XAF", "Treasury Clearing XAF", "XAF",
            TreasuryAccountType.Clearing, CancellationToken.None);

        cashId = cash.AccountId;
        clearingId = clearing.AccountId;

        var posted = await service.PostAsync(
            "TREASURY-DURABLE-001",
            "corr-treasury-durable-001",
            cashId,
            clearingId,
            "XAF",
            5_000_000,
            CancellationToken.None);

        transactionId = posted.TransactionId;
        Assert(posted.Status == TreasuryTransactionStatus.Posted, "Initial transaction must be posted.");
        Assert(posted.Entries.Sum(x => x.DebitMinor) == posted.Entries.Sum(x => x.CreditMinor),
            "Initial transaction must preserve double-entry.");

        var reservation = await service.ReserveAsync(
            cashId,
            1_000_000,
            "reserve-durable-001",
            CancellationToken.None);
        reservationId = reservation.ReservationId;
        await service.ReleaseReservationAsync(reservationId, CancellationToken.None);
    }

    await using (var restartedDb = new TreasuryDbContext(
        new DbContextOptionsBuilder<TreasuryDbContext>().UseSqlite(connection).Options))
    {
        var repository = new EfTreasuryRepository(restartedDb);
        var service = new TreasuryLedgerService(repository);

        var cash = await repository.GetAccountAsync(cashId, CancellationToken.None);
        Assert(cash is not null && cash.Status == TreasuryAccountStatus.Active,
            "Treasury account must survive restart.");

        var replay = await service.PostAsync(
            "TREASURY-DURABLE-001",
            "corr-treasury-durable-001",
            cashId,
            clearingId,
            "XAF",
            5_000_000,
            CancellationToken.None);

        Assert(replay.TransactionId == transactionId,
            "Exact correlation replay must return the original transaction.");

        var entries = await repository.GetEntriesAsync(cashId, CancellationToken.None);
        Assert(entries.Count == 1,
            "Exact replay must not append duplicate ledger entries.");

        var balance = await service.GetBalanceAsync(cashId, CancellationToken.None);
        Assert(balance.NetMinor == 5_000_000,
            "Balance projection must survive restart without duplication.");

        var reservation = await repository.GetReservationAsync(reservationId, CancellationToken.None);
        Assert(reservation?.Status == TreasuryReservationStatus.Released,
            "Released reservation state must survive restart.");
        Assert(reservation.ReleasedAtUtc is not null,
            "Released reservation timestamp must survive restart.");

        var conflictRejected = false;
        try
        {
            await service.PostAsync(
                "TREASURY-DURABLE-DIFFERENT",
                "corr-treasury-durable-001",
                cashId,
                clearingId,
                "XAF",
                4_000_000,
                CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            conflictRejected = true;
        }

        Assert(conflictRejected,
            "Conflicting correlation replay must be rejected.");

        var restored = await repository.GetTransactionByCorrelationIdAsync(
            "corr-treasury-durable-001",
            CancellationToken.None);
        Assert(restored is not null && restored.TransactionId == transactionId,
            "Posted transaction must remain queryable after restart.");
        Assert(restored!.Entries.Sum(x => x.DebitMinor) == restored.Entries.Sum(x => x.CreditMinor),
            "Restored transaction must remain balanced.");
    }

    Console.WriteLine("AFW-BE-TREASURY-DURABILITY-1 scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
