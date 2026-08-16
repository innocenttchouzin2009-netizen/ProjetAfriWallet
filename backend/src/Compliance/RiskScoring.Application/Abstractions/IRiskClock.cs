namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public interface IRiskClock
{
    DateTimeOffset UtcNow { get; }
}