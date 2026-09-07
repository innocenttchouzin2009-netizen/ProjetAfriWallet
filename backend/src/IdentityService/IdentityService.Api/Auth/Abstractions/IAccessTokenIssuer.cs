namespace IdentityService.Api.Auth.Abstractions;

public interface IAccessTokenIssuer
{
    string Issue(
        Guid userId,
        Guid sessionId,
        long tokenVersion,
        DateTimeOffset expiresAtUtc);
}
