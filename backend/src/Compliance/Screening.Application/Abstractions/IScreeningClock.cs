namespace AfriWallet.Compliance.Screening.Application.Abstractions;

public interface IScreeningClock
{
    DateTimeOffset UtcNow { get; }
}