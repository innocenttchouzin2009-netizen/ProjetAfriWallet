namespace UniversalWallet.Api.Payments.Application.Settlements;

public sealed record SettlementProviderResult(bool Success, string? ProviderReference = null, string? FailureCode = null, string? FailureReason = null);

public enum SettlementProviderStatus
{
    Unknown,
    Pending,
    Settled,
    Failed
}

public interface ISettlementProvider
{
    string Channel { get; }

    Task<SettlementProviderResult> SettleAsync(SettlementRequest request, CancellationToken cancellationToken);
    Task<SettlementProviderStatus> GetStatusAsync(string providerReference, CancellationToken cancellationToken);
}

public sealed record SettlementRequest(
    Guid SettlementId,
    Guid TransferId,
    Guid PaymentIntentId,
    string SettlementReference,
    string CorrelationId);
