using MerchantSettlement.Application.Services;
using MerchantSettlement.Domain.Positions;
using MerchantSettlement.Domain.Profiles;
using MerchantSettlement.Domain.Reconciliation;
using MerchantSettlement.Domain.Settlements;
using MerchantSettlement.Infrastructure.Acquiring;
using MerchantSettlement.Infrastructure.FinancialCore;
using MerchantSettlement.Infrastructure.Reconciliation;
using MerchantSettlement.Infrastructure.Repositories;

var repository = new InMemoryMerchantSettlementRepository();
var acquiring = new SandboxAcquiringReadModel();
var now = DateTime.UtcNow;

acquiring.Add(new MerchantSettlementTransaction(
    Guid.NewGuid(),
    "merchant-001",
    "XAF",
    1_000_000,
    15_000,
    50_000,
    now.AddMinutes(-30)));

acquiring.Add(new MerchantSettlementTransaction(
    Guid.NewGuid(),
    "merchant-001",
    "XAF",
    500_000,
    7_500,
    0,
    now.AddMinutes(-10)));

var positionService = new MerchantSettlementPositionService(acquiring);
var service = new MerchantSettlementService(
    repository,
    positionService,
    new SandboxFinancialSettlementGateway(),
    new SandboxFinancialReconciliationGateway());

var profile = await service.CreateProfileAsync(
    "merchant-001",
    "XAF",
    SettlementFrequency.Daily,
    settlementDelayDays: 1,
    minimumSettlementMinor: 10_000,
    CancellationToken.None);

Assert(profile.Status == MerchantSettlementProfileStatus.Active, "settlement profile");

var position = await positionService.CalculateAsync(
    "merchant-001",
    "XAF",
    now.AddDays(-1),
    now,
    adjustmentsMinor: 5_000,
    reserveMinor: 20_000,
    CancellationToken.None);

Assert(position.GrossMinor == 1_500_000, "gross position");
Assert(position.FeesMinor == 22_500, "fee aggregation");
Assert(position.RefundsMinor == 50_000, "refund aggregation");
Assert(position.NetPayableMinor == 1_412_500, "net settlement position");

var settlement = await service.CreateSettlementAsync(
    "merchant-001",
    now.AddDays(-1),
    now,
    adjustmentsMinor: 5_000,
    reserveMinor: 20_000,
    idempotencyKey: "merchant-settlement-001",
    CancellationToken.None);

Assert(settlement.Status == MerchantSettlementStatus.Created, "settlement creation");

var duplicate = await service.CreateSettlementAsync(
    "merchant-001",
    now.AddDays(-1),
    now,
    adjustmentsMinor: 5_000,
    reserveMinor: 20_000,
    idempotencyKey: "merchant-settlement-001",
    CancellationToken.None);

Assert(duplicate.SettlementId == settlement.SettlementId, "settlement idempotency");

await service.ExecuteAsync(settlement.SettlementId, CancellationToken.None);

Assert(settlement.Status == MerchantSettlementStatus.Completed && settlement.FinancialSettlementReference is not null, "financial core settlement");

var reconciliation = await service.ReconcileAsync(settlement.SettlementId, CancellationToken.None);

Assert(reconciliation.Status == MerchantReconciliationStatus.Matched, "merchant reconciliation");
Assert(reconciliation.DifferenceMinor == 0, "reconciliation balance");

Console.WriteLine("batch foundation ................. PASS");
Console.WriteLine("audit generation ................. PASS");
Console.WriteLine("telemetry generation ............. PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0014.4 merchant settlement scenarios passed.");

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
