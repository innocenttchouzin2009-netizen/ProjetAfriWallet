namespace AfriWallet.PaymentPlatform.ReleaseCandidate.Validation;

public sealed class ReleaseValidationSummary
{
    private readonly List<ReleaseCheck> _checks = [];

    public IReadOnlyCollection<ReleaseCheck> Checks =>
        _checks.AsReadOnly();

    public int Total => _checks.Count;

    public int Passed =>
        _checks.Count(x => x.Passed);

    public int Failed =>
        _checks.Count(x => !x.Passed);

    public int Skipped { get; private set; }

    public bool Success =>
        Failed == 0 &&
        Skipped == 0;

    public void Add(
        string name,
        bool passed,
        string details = "PASS")
    {
        _checks.Add(
            new ReleaseCheck(
                name,
                passed,
                details));
    }

    public void Skip(
        string name,
        string details)
    {
        _checks.Add(
            new ReleaseCheck(
                name,
                false,
                details));

        Skipped++;
    }
}
