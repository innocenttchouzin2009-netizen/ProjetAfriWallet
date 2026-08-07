using TreasuryReleaseCandidate.Validation;

var repositoryRoot = Directory.GetCurrentDirectory();
var validator = new TreasuryRcValidator(repositoryRoot);
var summary = validator.Run();

foreach (var check in summary.Checks)
{
	Console.WriteLine($"{check.Name,-48} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine(summary.Success
	? "Treasury release-candidate scenario suite PASS"
	: "Treasury release-candidate scenario suite FAIL");

if (!summary.Success)
{
	Environment.ExitCode = 1;
}
