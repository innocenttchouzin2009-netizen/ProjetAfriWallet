namespace IdentityService.Api.Auth.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
