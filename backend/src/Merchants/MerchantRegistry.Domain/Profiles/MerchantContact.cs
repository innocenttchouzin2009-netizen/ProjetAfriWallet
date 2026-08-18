namespace AfriWallet.Merchants.Registry.Domain.Profiles;

public sealed record MerchantContact
{
    public MerchantContact(string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Merchant email is required.", nameof(email));

        Email = email.Trim();
        Phone = phone?.Trim();
    }

    public string Email { get; }
    public string? Phone { get; }
}
