using AfriWallet.BankingPlatform.BankingReadiness.Validation;

var validator = new BankingReadinessValidator();
var summary = validator.Run();

foreach (var check in summary.Checks)
{
    Console.WriteLine($"{check.Name,-46} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Total}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();
Console.WriteLine(summary.Success ? "Decision: READY FOR BANKING RC" : "Decision: NOT READY");

if (!summary.Success)
{
    Environment.ExitCode = 1;
}
