using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Logging;

public sealed class MobileMoneyLoggingScope
{
    public MobileMoneyLoggingScope(CorrelationContext correlationContext)
    {
        CorrelationContext = correlationContext;
    }

    public CorrelationContext CorrelationContext { get; }

    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["correlationId"] = CorrelationContext.CorrelationId,
            ["transactionId"] = CorrelationContext.TransactionId,
            ["providerReference"] = CorrelationContext.ProviderReference,
            ["awidId"] = CorrelationContext.AwidId,
            ["walletId"] = CorrelationContext.WalletId,
            ["providerCode"] = CorrelationContext.ProviderCode,
            ["operationType"] = CorrelationContext.OperationType
        };
    }
}
