namespace AfriWallet.BankingPlatform.BankingReadiness.Validation;

public sealed class ReadinessValidationSummary
{
    private readonly List<ReadinessCheck> _checks = [];

    public IReadOnlyCollection<ReadinessCheck> Checks => _checks.AsReadOnly();
    public int Total => _checks.Count;
    public int Passed => _checks.Count(x => x.Passed);
    public int Failed => _checks.Count(x => !x.Passed);
    public int Skipped { get; private set; }
    public bool Success => Failed == 0 && Skipped == 0;

    public void Add(string name, bool passed, string details = "PASS")
    {
        _checks.Add(new ReadinessCheck(name, passed, details));
    }

    public void Skip(string name, string details)
    {
        _checks.Add(new ReadinessCheck(name, false, details));
        Skipped++;
    }
}
