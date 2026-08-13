using AfriWallet.BankingPlatform.BankingReadiness.Validation;

static void Check(
    string name,
    bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-44} FAIL");
        throw new InvalidOperationException($"Readiness scenario failed: {name}");
    }

    Console.WriteLine($"{name,-44} PASS");
}

var validator = new BankingReadinessValidator();
var summary = validator.Run();

Check("all readiness checks executed", summary.Total == 28);
Check("all readiness checks passed", summary.Passed == 28);
Check("no readiness failures", summary.Failed == 0);
Check("no readiness skips", summary.Skipped == 0);
Check("banking RC decision", summary.Success);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0015.7 banking readiness scenarios passed.");
Console.WriteLine();
Console.WriteLine("Decision: READY FOR BANKING RC");
