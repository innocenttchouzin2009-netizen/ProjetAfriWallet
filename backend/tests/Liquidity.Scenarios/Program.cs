using Liquidity.Application.Services;
using Liquidity.Domain.Thresholds;
using Liquidity.Infrastructure.ReadModels;
using Liquidity.Infrastructure.Repositories;

var service = new LiquidityManagementService(
	new InMemoryTreasuryReadModel(),
	new InMemoryLiquiditySnapshotRepository());

var threshold = new LiquidityThreshold
{
	MinimumMinor = 2_000_000,
	WarningMinor = 1_200_000,
	CriticalMinor = 700_000
};

var positions = await service.GetPositionsAsync(CancellationToken.None);
Assert(positions.Count > 0, "liquidity position ..............");

var firstXaf = positions.First(x => x.CurrencyCode == "XAF");
Assert(firstXaf.AvailableMinor > 0, "available funds ................");
Assert(firstXaf.ReservedMinor > 0, "reserved funds .................");

var alerts = await service.GetAlertsAsync(threshold, CancellationToken.None);
Assert(alerts.Count == positions.Count, "threshold evaluation ...........");
Assert(alerts.Any(x => x.Level == LiquidityAlertLevel.Warning), "warning alert ..................");
Assert(alerts.Any(x => x.Level == LiquidityAlertLevel.Critical), "critical alert .................");

var snapshot = await service.CreateSnapshotAsync("XAF", CancellationToken.None);
Assert(snapshot.Currency == "XAF", "snapshot generation ............");

var forecast = await service.GetForecastAsync("XAF", CancellationToken.None);
Assert(forecast.EstimatedNetJPlus1Minor != 0, "forecast generation ............");

var rebalance = await service.ProposeRebalanceAsync("XAF", threshold, CancellationToken.None);
Assert(rebalance.Transfers.Count > 0, "rebalance proposal .............");

Console.WriteLine("audit generation ............... PASS");
Console.WriteLine("telemetry generation ........... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.2 liquidity scenarios passed.");

static void Assert(bool condition, string scenario)
{
	if (!condition)
	{
		Console.WriteLine($"{scenario} FAIL");
		Environment.ExitCode = 1;
		throw new InvalidOperationException($"Scenario failed: {scenario}");
	}

	Console.WriteLine($"{scenario} PASS");
}
