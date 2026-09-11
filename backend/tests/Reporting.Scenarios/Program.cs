using Reporting.Application.Services;
using Reporting.Infrastructure.DataSources;

var dataSource = new InMemoryReportingDataSource();
var service = new ReportingDashboardService(dataSource);

var snapshot = await service.BuildExecutiveDashboardAsync(
    DateTime.UtcNow.AddDays(-30),
    DateTime.UtcNow,
    CancellationToken.None);

Assert(snapshot.Metrics.Any(x => x.MetricCode == "PAYMENT_VOLUME"), "payment volume");
Assert(snapshot.Metrics.Any(x => x.MetricCode == "TRANSACTION_COUNT"), "transaction count");
Assert(snapshot.Metrics.Any(x => x.MetricCode == "ACTIVE_MERCHANTS"), "merchant analytics");
Assert(snapshot.Metrics.Any(x => x.MetricCode == "OPEN_SUPPORT_CASES"), "support analytics");
Assert(snapshot.GeneratedAtUtc <= DateTime.UtcNow, "dashboard generation");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0012.4 reporting and BI scenarios passed.");

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
