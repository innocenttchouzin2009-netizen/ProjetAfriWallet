namespace Subscriptions.Api.Configuration;

public interface ISecretProvider
{
    string? GetSecret(string key);
}
