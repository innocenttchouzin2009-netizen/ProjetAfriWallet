namespace MobileMoney.Production.BeneficiaryResolution.Application;

public sealed class BeneficiaryResolutionException : Exception
{
    public BeneficiaryResolutionException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
