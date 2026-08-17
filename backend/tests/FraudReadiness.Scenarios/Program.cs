using AfriWallet.Fraud.Readiness.Checks;
using AfriWallet.Fraud.Readiness.Models;
using AfriWallet.Fraud.Readiness.Services;

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) && Directory.Exists(Path.Combine(current.FullName, "backend"))) return current.FullName;
        current = current.Parent;
    }
    throw new InvalidOperationException("AfriWallet repository root not found.");
}

var root = args.Length > 0 && Directory.Exists(Path.Combine(args[0], "backend")) ? Path.GetFullPath(args[0]) : FindRepositoryRoot();
Console.WriteLine("\nAFW-DLV-0017.7 - Fraud Platform Production Readiness\n");
IFraudReadinessCheck[] checks = [new DeliveryPresenceCheck(), new FrozenTagCheck(), new ArchitectureBoundaryCheck(), new ExecutionBoundaryCheck(), new SecretBoundaryCheck(), new MachineLearningBoundaryCheck(), new AuditCapabilityCheck()];
var report = await new FraudReadinessRunner(checks).RunAsync(root);
foreach (var check in report.Checks)
{
    var status = check.Status switch { ReadinessStatus.Passed => "PASS", ReadinessStatus.Failed => "FAIL", _ => "SKIP" };
    Console.WriteLine($"{check.Code,-14}{check.Name,-40}{status}");
    Console.WriteLine($"  Evidence: {check.Evidence}");
}
Console.WriteLine($"\nChecks:  {report.Total}\nPassed:  {report.Passed}\nFailed:  {report.Failed}\nSkipped: {report.Skipped}\n");
if (!report.IsReady) { Console.WriteLine("Decision: NOT READY"); Environment.ExitCode = 1; return; }
Console.WriteLine("Fraud Platform Readiness PASS");
Console.WriteLine("Machine learning: NOT IMPLEMENTED");
Console.WriteLine("Automatic enforcement: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR FRAUD RC");