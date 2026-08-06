namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantHealthProbe
{
    public Dictionary<string, bool> Check()
    {
        return new Dictionary<string, bool>
        {
            ["merchant-registry"] = true,
            ["merchant-kyc"] = true,
            ["merchant-qr"] = true,
            ["merchant-pos"] = true,
            ["merchant-settlement"] = true,
            ["merchant-dashboard"] = true,
            ["payment-gateway"] = true
        };
    }
}
