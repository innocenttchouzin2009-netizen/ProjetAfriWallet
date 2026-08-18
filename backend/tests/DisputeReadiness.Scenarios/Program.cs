using AfriWallet.Disputes.Readiness.Checks;
using AfriWallet.Disputes.Readiness.Models;
using AfriWallet.Disputes.Readiness.Services;

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
            Directory.Exists(Path.Combine(current.FullName, "backend")))
        {
            return current.FullName;
        }
        current = current.Parent;
    }

    throw new InvalidOperationException("AfriWallet repository root not found.");
}

var root = args.Length > 0 ? args[0] : FindRepositoryRoot();

Console.WriteLine();
Console.WriteLine("AFW-DLV-0018.7 - Dispute Platform Production Readiness");
Console.WriteLine();

IDisputeReadinessCheck[] checks =
[
    new DeliveryPresenceCheck(),
    new FrozenTagCheck(),
    new ArchitectureBoundaryCheck(),
    new FinancialBoundaryCheck(),
    new LedgerBoundaryCheck(),
    new AuditCapabilityCheck(),
    new DeterministicIntelligenceCheck(),
    new SecretBoundaryCheck()
];

var report = await new DisputeReadinessRunner(checks).RunAsync(root);

foreach (var check in report.Checks)
{
    var status = check.Status switch
    {
        ReadinessStatus.Passed => "PASS",
        ReadinessStatus.Failed => "FAIL",
        _ => "SKIP"
    };
    Console.WriteLine($"{check.Code,-14}{check.Name,-42}{status}");
    Console.WriteLine($"  Evidence: {check.Evidence}");
}

Console.WriteLine();
Console.WriteLine($"Checks:  {report.Total}");
Console.WriteLine($"Passed:  {report.Passed}");
Console.WriteLine($"Failed:  {report.Failed}");
Console.WriteLine($"Skipped: {report.Skipped}");
Console.WriteLine();

if (!report.IsReady)
{
    Console.WriteLine("Decision: NOT READY");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("Dispute Platform Readiness PASS");
Console.WriteLine("Real refund execution: NOT IMPLEMENTED");
Console.WriteLine("Real chargeback submission: NOT IMPLEMENTED");
Console.WriteLine("Automatic merchant blocking: NOT IMPLEMENTED");
Console.WriteLine("Automatic customer suspension: NOT IMPLEMENTED");
Console.WriteLine("Direct ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR DISPUTE RC");
