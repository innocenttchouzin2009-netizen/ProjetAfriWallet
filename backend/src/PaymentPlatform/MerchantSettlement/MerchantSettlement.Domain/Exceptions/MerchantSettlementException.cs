namespace MerchantSettlement.Domain.Exceptions;

public sealed class MerchantSettlementException : Exception
{
    public MerchantSettlementException(string message)
        : base(message)
    {
    }
}
