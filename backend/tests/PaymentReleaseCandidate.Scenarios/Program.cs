using AfriWallet.PaymentPlatform.ReleaseCandidate.Validation;

static void Assert(
    string name,
    bool condition)
{
    if (!condition)
    {
        Console.WriteLine(
            $"{name,-42} FAIL");

        throw new InvalidOperationException(
            $"RC scenario failed: {name}");
    }

    Console.WriteLine(
        $"{name,-42} PASS");
}

var validator =
    new PaymentRcValidator();

var summary =
    validator.Run();

Assert(
    "all RC checks executed",
    summary.Total == 29);

Assert(
    "all RC checks passed",
    summary.Passed == 29);

Assert(
    "no RC failures",
    summary.Failed == 0);

Assert(
    "no RC skips",
    summary.Skipped == 0);

Assert(
    "payment platform RC decision",
    summary.Success);

Console.WriteLine();
Console.WriteLine(
    "All AFW-DLV-0014.8 Payment Platform RC scenarios passed.");

Console.WriteLine();
Console.WriteLine(
    "Decision: READY FOR PAYMENT RC");
