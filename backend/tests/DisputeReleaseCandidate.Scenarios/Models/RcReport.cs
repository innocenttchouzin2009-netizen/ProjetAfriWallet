namespace AfriWallet.Disputes.ReleaseCandidate.Models;

public sealed class RcReport
{
    public RcReport(IReadOnlyCollection<RcCheck> checks)
    {
        Checks = checks;
    }

    public IReadOnlyCollection<RcCheck> Checks { get; }
    public int Total => Checks.Count;
    public int Passed => Checks.Count(x => x.Passed);
    public int Failed => Checks.Count(x => !x.Passed);
    public int Skipped => 0;
    public bool Ready => Total > 0 && Failed == 0 && Passed == Total;
}
