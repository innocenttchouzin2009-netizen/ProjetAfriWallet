using AfriWallet.BankingPlatform.BankingReleaseCandidate.Validation;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-46} FAIL");
        throw new InvalidOperationException($"Banking RC scenario failed: {name}");
    }

    Console.WriteLine($"{name,-46} PASS");
}

var validator = new BankingRcValidator();
var summary = validator.Run();

Check("all banking RC checks executed", summary.Total == 30);
Check("all banking RC checks passed", summary.Passed == 30);
Check("no banking RC failures", summary.Failed == 0);
Check("no banking RC skips", summary.Skipped == 0);
Check("banking RC decision", summary.Success);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0015.8 Banking Platform RC scenarios passed.");
Console.WriteLine();
Console.WriteLine("Decision: READY FOR BANKING RC");
