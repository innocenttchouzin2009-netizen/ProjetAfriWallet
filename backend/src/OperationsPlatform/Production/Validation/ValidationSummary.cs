namespace Operations.Platform.Validation;

public sealed class ValidationSummary
{
    private readonly List<ValidationResult> _results = new();

    public IReadOnlyCollection<ValidationResult> Results => _results;

    public int Passed => _results.Count(x => x.Passed);

    public int Failed => _results.Count(x => !x.Passed);

    public int Total => _results.Count;

    public void Add(
        string check,
        bool passed,
        string message = "")
    {
        _results.Add(
            new ValidationResult(
                check,
                passed,
                message));
    }

    public bool Success =>
        Failed == 0;
}
