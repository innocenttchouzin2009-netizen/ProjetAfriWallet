namespace AfriWallet.PaymentPlatform.ReleaseCandidate.Validation;

public sealed record ReleaseCheck(
    string Name,
    bool Passed,
    string Details);
