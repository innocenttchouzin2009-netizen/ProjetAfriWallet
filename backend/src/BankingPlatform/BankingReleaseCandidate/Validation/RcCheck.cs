namespace AfriWallet.BankingPlatform.BankingReleaseCandidate.Validation;

public sealed record RcCheck(
    string Name,
    bool Passed,
    string Details);
