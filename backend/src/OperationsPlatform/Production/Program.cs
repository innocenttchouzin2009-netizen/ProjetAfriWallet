using Operations.Platform.Checks;

static string FormatCheck(string check, int width = 30)
{
    if (check.Length >= width)
    {
        return check;
    }

    return check + " " + new string('.', width - check.Length - 1);
}

var validator =
    new ProductionValidator();

var summary =
    validator.Execute();

foreach (var result in summary.Results)
{
    Console.WriteLine(
        $"{FormatCheck(result.Check)} {(result.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();

Console.WriteLine(
    $"Checks: {summary.Total}");

Console.WriteLine(
    $"Passed: {summary.Passed}");

Console.WriteLine(
    $"Failed: {summary.Failed}");

Console.WriteLine(
    "Skipped: 0");

Console.WriteLine();

Console.WriteLine(
    summary.Success
        ? "Decision: READY FOR OPERATIONS RC"
        : "Decision: NOT READY");
