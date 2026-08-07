namespace Operations.ReleaseCandidate.Validation;

public sealed class ReleaseValidationSummary
{
    private readonly List<ReleaseCheck> _checks = [];

    public IReadOnlyList<ReleaseCheck> Checks => _checks;

    public int Total => _checks.Count;

    public int Passed => _checks.Count(x => x.Passed);

    public int Failed => _checks.Count(x => !x.Passed);

    public int Skipped { get; private set; }

    public bool Success => Failed == 0 && Skipped == 0;

    public void Add(
        string name,
        bool passed,
        string? details = null)
    {
        _checks.Add(new ReleaseCheck(
            name,
            passed,
            details));
    }

    public void Skip()
    {
        Skipped++;
    }
}
