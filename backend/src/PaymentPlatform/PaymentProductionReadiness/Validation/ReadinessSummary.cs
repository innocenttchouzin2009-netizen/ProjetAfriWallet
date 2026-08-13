namespace AfriWallet.PaymentPlatform.ProductionReadiness.Validation;

public sealed class ReadinessSummary
{
    private readonly List<ReadinessCheckResult> _checks = [];

    public IReadOnlyCollection<ReadinessCheckResult> Checks => _checks.AsReadOnly();

    public int Total => _checks.Count;

    public int Passed => _checks.Count(check => check.Passed);

    public int Failed => _checks.Count(check => !check.Passed);

    public int Skipped { get; private set; }

    public bool Success => Failed == 0 && Skipped == 0;

    public void Add(string name, bool passed, string details = "PASS")
    {
        _checks.Add(new ReadinessCheckResult(name, passed, details));
    }

    public void Skip()
    {
        Skipped++;
    }
}