using System.Linq;

namespace TreasuryProduction.Validation;

public sealed class ProductionValidationSummary
{
    private readonly List<ProductionCheckResult> _results = [];

    public IReadOnlyCollection<ProductionCheckResult> Results =>
        _results.AsReadOnly();

    public int Checks => _results.Count;

    public int Passed => _results.Count(x => x.Passed);

    public int Failed => _results.Count(x => !x.Passed);

    public int Skipped { get; private set; }

    public bool Success =>
        Failed == 0 && Skipped == 0;

    public void Add(
        string name,
        bool passed,
        string details)
    {
        _results.Add(
            new ProductionCheckResult(
                name,
                passed,
                details));
    }

    public void Skip() =>
        Skipped++;
}