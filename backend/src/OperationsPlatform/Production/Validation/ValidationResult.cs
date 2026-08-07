namespace Operations.Platform.Validation;

public sealed record ValidationResult(
    string Check,
    bool Passed,
    string Message);
