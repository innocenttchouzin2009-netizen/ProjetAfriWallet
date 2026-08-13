namespace AfriWallet.PaymentPlatform.MobileMoney.Domain;

public sealed class MobileMoneyException : Exception
{
    public string Code { get; }

    public MobileMoneyException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}