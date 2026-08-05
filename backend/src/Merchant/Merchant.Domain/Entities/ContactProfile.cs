namespace AfriWallet.Merchant.Domain.Entities;

public sealed class ContactProfile
{
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}