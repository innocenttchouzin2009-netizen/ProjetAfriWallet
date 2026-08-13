using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;

namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;

public sealed class BankBeneficiary
{
    private readonly List<ExternalBankAccount> _accounts = [];

    public BankBeneficiary(
        Guid beneficiaryId,
        string ownerAwid,
        string displayName,
        BeneficiaryType type)
    {
        if (beneficiaryId == Guid.Empty)
            throw new ArgumentException("Beneficiary ID is required.");

        BeneficiaryId = beneficiaryId;
        OwnerAwid = Require(ownerAwid);
        DisplayName = Require(displayName);
        Type = type;
    }

    public Guid BeneficiaryId { get; }

    public string OwnerAwid { get; }

    public string DisplayName { get; private set; }

    public BeneficiaryType Type { get; }

    public BeneficiaryStatus Status { get; private set; } = BeneficiaryStatus.Active;

    public IReadOnlyCollection<ExternalBankAccount> Accounts => _accounts.AsReadOnly();

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public void AddAccount(ExternalBankAccount account)
    {
        EnsureActive();

        if (_accounts.Any(x =>
                x.Identifier.Type == account.Identifier.Type &&
                string.Equals(
                    x.Identifier.Value,
                    account.Identifier.Value,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Bank account already exists on beneficiary.");
        }

        _accounts.Add(account);
    }

    public void Rename(string displayName)
    {
        EnsureActive();
        DisplayName = Require(displayName);
    }

    public void Disable()
    {
        Status = BeneficiaryStatus.Disabled;
    }

    private void EnsureActive()
    {
        if (Status != BeneficiaryStatus.Active)
            throw new InvalidOperationException("Beneficiary is not active.");
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }
}

public enum BeneficiaryType
{
    Individual,
    Business
}

public enum BeneficiaryStatus
{
    Active,
    Disabled
}
