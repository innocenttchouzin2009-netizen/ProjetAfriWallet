namespace AfriWallet.Banking.Api.Production.Configuration;

public sealed class BankingProductionSecretValidator
{
    public bool Validate(string[] requiredSecrets, IReadOnlyDictionary<string, string?> values)
    {
        foreach (var secret in requiredSecrets)
        {
            if (!values.TryGetValue(secret, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }
}
