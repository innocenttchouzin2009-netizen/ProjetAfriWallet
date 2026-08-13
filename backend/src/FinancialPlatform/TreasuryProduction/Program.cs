using TreasuryProduction.Checks;

var validator = new TreasuryProductionValidator();
var summary = validator.Run();

foreach (var result in summary.Results)
{
	Console.WriteLine($"{result.Name,-38} {(result.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Checks}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();
Console.WriteLine(summary.Success ? "Decision: READY FOR TREASURY RC" : "Decision: NOT READY");

if (!summary.Success)
{
	Environment.ExitCode = 1;
}
