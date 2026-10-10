namespace MobileMoney.Production.Authentication;

public sealed record AfWalValidatedSession(
    Guid UserId,
    Guid SessionId,
    string DeviceId,
    DateTimeOffset ExpiresAtUtc,
    long TokenVersion)
{
    public static readonly object HttpContextItemKey = new();
}
