using Microsoft.AspNetCore.Http;

namespace MobileMoney.Production.Correlation;

public sealed class CorrelationContext
{
    public const string ItemKey = "MobileMoney.CorrelationContext";

    public CorrelationContext(
        string correlationId,
        string? transactionId = null,
        string? providerReference = null,
        string? awidId = null,
        string? walletId = null,
        string? providerCode = null,
        string? operationType = null)
    {
        CorrelationId = correlationId;
        TransactionId = transactionId;
        ProviderReference = providerReference;
        AwidId = awidId;
        WalletId = walletId;
        ProviderCode = providerCode;
        OperationType = operationType;
    }

    public string CorrelationId { get; }

    public string? TransactionId { get; }

    public string? ProviderReference { get; }

    public string? AwidId { get; }

    public string? WalletId { get; }

    public string? ProviderCode { get; }

    public string? OperationType { get; }

    public static CorrelationContext? FromHttpContext(HttpContext? httpContext)
    {
        if (httpContext?.Items.TryGetValue(ItemKey, out var item) == true && item is CorrelationContext context)
        {
            return context;
        }

        return null;
    }
}
