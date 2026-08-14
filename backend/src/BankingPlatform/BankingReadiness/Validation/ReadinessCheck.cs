namespace AfriWallet.BankingPlatform.BankingReadiness.Validation;

public sealed record ReadinessCheck(
    string Name,
    bool Passed,
    string Details);
