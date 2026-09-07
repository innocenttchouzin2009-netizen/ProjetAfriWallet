namespace IdentityService.Api.Auth.Abstractions;

public interface IRefreshTokenService
{
    string Generate();

    string Hash(string refreshToken);
}
