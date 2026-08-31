namespace AfriWallet.Merchants.Intelligence.Domain.Findings;
public sealed record MerchantRiskPattern
{
    public MerchantRiskPattern(string code, int score, string reason, IReadOnlyCollection<string> references)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Pattern code is required.", nameof(code));
        if (score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(score));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Pattern reason is required.", nameof(reason));
        Code = code.Trim(); Score = score; Reason = reason.Trim(); References = references ?? throw new ArgumentNullException(nameof(references));
    }
    public string Code { get; }
    public int Score { get; }
    public string Reason { get; }
    public IReadOnlyCollection<string> References { get; }
}
