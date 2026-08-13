namespace AfriWallet.PaymentPlatform.ProductionReadiness.Validation;

public sealed record ReadinessCheckResult(
    string Name,
    bool Passed,
    string Details);