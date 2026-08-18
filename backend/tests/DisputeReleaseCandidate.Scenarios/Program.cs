using AfriWallet.Disputes.ReleaseCandidate.Services;

var root = args.Length > 0 ? args[0] : RepositoryRootResolver.Resolve();

Console.WriteLine();
Console.WriteLine("AFW-DLV-0018.8 - Dispute Platform Release Candidate v1.8.0-rc1");
Console.WriteLine();

var git = new GitTagVerifier(root);
var runner = new DisputeRcRunner(git);
var report = runner.Run();

foreach (var check in report.Checks)
{
    Console.WriteLine($"{check.Code,-28}{check.Name,-42}{(check.Passed ? "PASS" : "FAIL")}");
    Console.WriteLine($"  Evidence: {check.Evidence}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {report.Total}");
Console.WriteLine($"Passed: {report.Passed}");
Console.WriteLine($"Failed: {report.Failed}");
Console.WriteLine($"Skipped: {report.Skipped}");
Console.WriteLine();

if (!report.Ready)
{
    Console.WriteLine("Decision: NOT READY");
    Environment.ExitCode = 1;
    return;
}

await new DisputeRcPackageBuilder().BuildAsync(root, report);

Console.WriteLine("Dispute RC Scenario Runner PASS");
Console.WriteLine("Dispute RC Packaging PASS");
Console.WriteLine("Real refund execution: NOT IMPLEMENTED");
Console.WriteLine("Real chargeback submission: NOT IMPLEMENTED");
Console.WriteLine("Direct ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR DISPUTE RC");
