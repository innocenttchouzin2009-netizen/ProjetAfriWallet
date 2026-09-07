using IdentityService.Api.Auth.Abstractions;

namespace IdentityService.Api.Auth.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
