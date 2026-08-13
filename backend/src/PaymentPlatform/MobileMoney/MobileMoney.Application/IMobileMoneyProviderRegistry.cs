namespace AfriWallet.PaymentPlatform.MobileMoney.Application;

public interface IMobileMoneyProviderRegistry
{
    IReadOnlyCollection<IMobileMoneyProvider> GetAll();

    IMobileMoneyProvider GetRequired(string providerCode);

    bool TryGet(
        string providerCode,
        out IMobileMoneyProvider? provider);
}