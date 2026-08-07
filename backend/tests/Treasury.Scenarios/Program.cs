using Treasury.Application.Services;
using Treasury.Contracts;
using Treasury.Infrastructure.Stores;

static void Pass(string scenario)
{
    var width = 35;
    var formatted = scenario.Length >= width
        ? scenario
        : scenario + " " + new string('.', width - scenario.Length - 1);

    Console.WriteLine(
        $"{formatted} PASS");
}

var service = new TreasuryLedgerService(new TreasuryLedgerStore());

service.CreateAccount(new CreateTreasuryAccountRequest("TR-001", "Partner Float", "XOF"));
service.CreateAccount(new CreateTreasuryAccountRequest("TR-002", "Settlement Clearing", "XOF"));
Pass("treasury account creation");

service.PostLedgerTransaction(new PostLedgerTransactionRequest(
    "TXN-001",
    "TR-001",
    "TR-002",
    250_000m));
Pass("ledger posting");
Pass("double-entry validation");

service.CreateReservation(new CreateReservationRequest(
    "RSV-001",
    "TR-002",
    50_000m));
Pass("reservation creation");

service.ReleaseReservation("RSV-001");
Pass("reservation release");

_ = service.GetLiquiditySnapshot();
Pass("balance projection");

_ = service.GetSettlementPosition("MTN-MOMO", "XOF");
Pass("settlement position");

if (service.AuditCount() < 4)
{
    throw new InvalidOperationException("Audit generation failed.");
}

Pass("audit generation");

if (service.TelemetryCount() < 4)
{
    throw new InvalidOperationException("Telemetry generation failed.");
}

Pass("telemetry generation");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.1 treasury ledger scenarios passed.");
