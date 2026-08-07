namespace TreasuryProduction.Validation;

public sealed record ProductionCheckResult(
    string Name,
    bool Passed,
    string Details);