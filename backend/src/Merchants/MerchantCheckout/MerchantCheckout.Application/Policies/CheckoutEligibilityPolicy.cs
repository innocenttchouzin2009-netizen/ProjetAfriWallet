using AfriWallet.Merchants.Checkout.Application.Abstractions;

namespace AfriWallet.Merchants.Checkout.Application.Policies;

public sealed class CheckoutEligibilityPolicy
{
    public void EnsureEligible(MerchantCommerceEligibilitySnapshot merchant)
    {
        if (!string.Equals(merchant.RegistryStatus, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Merchant registry status must be Active.");
        if (!string.Equals(merchant.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Merchant must be verified before checkout creation.");
    }
}
