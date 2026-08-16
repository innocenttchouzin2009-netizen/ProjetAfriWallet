using AfriWallet.Compliance.Screening.Application.Abstractions;

namespace AfriWallet.Compliance.Screening.Infrastructure;

public sealed class SystemScreeningClock : IScreeningClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}