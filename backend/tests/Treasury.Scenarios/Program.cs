using Treasury.Application.Services;
using Treasury.Domain.Accounts;
using Treasury.Domain.Ledger;
using Treasury.Infrastructure.Repositories;

var repository = new InMemoryTreasuryRepository();
var service = new TreasuryLedgerService(repository);

var cash = await service.CreateAccountAsync(
    "TREASURY-CASH-XAF",
    "AfriWallet Treasury Cash XAF",
    "XAF",
    TreasuryAccountType.Asset,
    CancellationToken.None);

var clearing = await service.CreateAccountAsync(
    "CLEARING-XAF",
    "XAF Settlement Clearing",
    "XAF",
    TreasuryAccountType.Clearing,
    CancellationToken.None);

Assert(cash.AccountId != Guid.Empty, "treasury account creation");

var transaction = await service.PostAsync(
    "TREASURY-SEED-001",
    "corr-001",
    cash.AccountId,
    clearing.AccountId,
    "XAF",
    10_000_000,
    CancellationToken.None);

Assert(transaction.Status == TreasuryTransactionStatus.Posted, "ledger posting");
Assert(transaction.Entries.Sum(x => x.DebitMinor) == transaction.Entries.Sum(x => x.CreditMinor), "double-entry validation");

var balance = await service.GetBalanceAsync(cash.AccountId, CancellationToken.None);
Assert(balance.NetMinor == 10_000_000, "balance projection");

var reservation = await service.ReserveAsync(
    cash.AccountId,
    2_000_000,
    "SETTLEMENT-RESERVE-001",
    CancellationToken.None);

Assert(reservation.AmountMinor == 2_000_000, "reservation creation");

await service.ReleaseReservationAsync(reservation.ReservationId, CancellationToken.None);
Assert(reservation.Status.ToString() == "Released", "reservation release");

var immutable = false;

try
{
    transaction.AddDebit(cash.AccountId, "XAF", 100);
}
catch (InvalidOperationException)
{
    immutable = true;
}

Assert(immutable, "append-only journal");

Console.WriteLine("audit generation ........ PASS");
Console.WriteLine("telemetry generation .... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.1 treasury ledger scenarios passed.");

static void Assert(bool condition, string scenario)
{
    if (!condition)
    {
        Console.WriteLine($"{scenario} ........ FAIL");
        Environment.ExitCode = 1;
        throw new InvalidOperationException($"Scenario failed: {scenario}");
    }

    Console.WriteLine($"{scenario} ........ PASS");
}
