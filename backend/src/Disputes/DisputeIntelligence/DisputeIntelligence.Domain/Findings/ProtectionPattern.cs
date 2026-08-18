namespace AfriWallet.Disputes.Intelligence.Domain.Findings;

public sealed record ProtectionPattern
{
    public ProtectionPattern(string code, int score, string reason, IReadOnlyCollection<string> references)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Pattern code is required.", nameof(code));
        if (score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(score));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Pattern reason is required.", nameof(reason));
        ArgumentNullException.ThrowIfNull(references);

        Code = code.Trim();
        Score = score;
        Reason = reason.Trim();
        References = references;
    }

    public string Code { get; }
    public int Score { get; }
    public string Reason { get; }
    public IReadOnlyCollection<string> References { get; }
}
