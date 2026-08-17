using AfriWallet.Fraud.ReleaseCandidate.Services;

var root = RepositoryRootResolver.Resolve(args.Length > 0 ? args[0] : null);
Console.WriteLine("\nAFW-DLV-0017.8 - Fraud Platform Release Candidate v1.7.0-rc1\n");
var report = new FraudRcRunner(root, new GitDeliveryVerifier(root)).Run();
foreach (var check in report.Checks)
{
    Console.WriteLine($"{check.Code,-28}{check.Name,-38}{(check.Passed ? "PASS" : "FAIL")}");
    Console.WriteLine($"  Evidence: {check.Evidence}");
}
Console.WriteLine($"\nChecks: {report.Total}\nPassed: {report.Passed}\nFailed: {report.Failed}\nSkipped: {report.Skipped}\n");
if (!report.Ready) { Console.WriteLine("Decision: NOT READY"); Environment.ExitCode = 1; return; }
await new FraudRcPackageBuilder().BuildAsync(root, report);
Console.WriteLine("Fraud RC Scenario Runner PASS");
Console.WriteLine("Fraud RC Packaging PASS");
Console.WriteLine("Opaque ML: NOT IMPLEMENTED");
Console.WriteLine("Automatic enforcement: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR FRAUD RC");