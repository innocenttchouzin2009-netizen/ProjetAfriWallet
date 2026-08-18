namespace AfriWallet.Disputes.Decision.Domain.Decisions;

public sealed record DecisionFactor
{
    public DecisionFactor(string code, string description, string source)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Factor code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Factor description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Factor source is required.", nameof(source));

        Code = code.Trim();
        Description = description.Trim();
        Source = source.Trim();
    }

    public string Code { get; }
    public string Description { get; }
    public string Source { get; }
}
