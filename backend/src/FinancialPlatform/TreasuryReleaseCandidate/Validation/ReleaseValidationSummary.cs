namespace TreasuryReleaseCandidate.Validation;

public sealed class ReleaseValidationSummary
{
    public ReleaseValidationSummary(IReadOnlyList<ReleaseCheck> checks)
    {
        Checks = checks;
    }

    public IReadOnlyList<ReleaseCheck> Checks { get; }

    public int Passed => Checks.Count(check => check.Passed);

    public int Failed => Checks.Count(check => !check.Passed);

    public int Skipped => 0;

    public bool Success => Failed == 0;
}
