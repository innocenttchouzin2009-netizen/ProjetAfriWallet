namespace MobileMoney.Production.Secrets;

public interface ISecretProvider
{
    string? GetSecret(string key);
    bool HasSecret(string key);
}
