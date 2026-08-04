using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Domain.Settlements;
using UniversalWallet.Api.Payments.Domain.Transfers;

namespace UniversalWallet.Api.Payments.Application.Settlements;

public sealed record CreateSettlementRequest(Guid TransferId, SettlementChannel Channel);

public sealed record CreateSettlementResponse(Guid SettlementId, Guid TransferId, SettlementChannel Channel, SettlementStatus Status, string SettlementReference, DateTimeOffset? SettledAt);

public interface ISettlementRepository
{
    Task<Settlement?> GetByTransferIdAsync(Guid transferId, CancellationToken cancellationToken = default);
    Task<Settlement?> GetAsync(Guid settlementId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Settlement>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default);
}

public sealed class CreateSettlementHandler
{
    private readonly IPaymentTransferRepository _transferRepository;
    private readonly ISettlementRepository _settlementRepository;
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly ISettlementProvider _settlementProvider;

    public CreateSettlementHandler(
        IPaymentTransferRepository transferRepository,
        ISettlementRepository settlementRepository,
        IPaymentIntentRepository intentRepository,
        ISettlementProvider settlementProvider)
    {
        _transferRepository = transferRepository;
        _settlementRepository = settlementRepository;
        _intentRepository = intentRepository;
        _settlementProvider = settlementProvider;
    }

    public async Task<CreateSettlementResponse> HandleAsync(CreateSettlementRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _settlementRepository.GetByTransferIdAsync(request.TransferId, cancellationToken);
        if (existing is not null)
        {
            return new CreateSettlementResponse(existing.SettlementId, existing.TransferId, existing.Channel, existing.Status, existing.SettlementReference, existing.SettledAt);
        }

        var transfer = await _transferRepository.GetAsync(request.TransferId, cancellationToken);
        if (transfer is null)
        {
            throw new InvalidOperationException("TRANSFER_NOT_FOUND");
        }

        if (transfer.Status != PaymentTransferStatus.Completed)
        {
            throw new InvalidOperationException("TRANSFER_NOT_COMPLETED");
        }

        var intent = await _intentRepository.GetAsync(transfer.PaymentIntentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        var settlement = new Settlement
        {
            TransferId = transfer.TransferId,
            PaymentIntentId = intent.Id,
            Channel = request.Channel,
            Status = SettlementStatus.PROCESSING,
            SettlementReference = BuildReference(),
            CorrelationId = transfer.CorrelationId,
            Version = 1
        };

        await _settlementRepository.AddAsync(settlement, cancellationToken);

        var providerResult = await _settlementProvider.SettleAsync(new SettlementRequest(settlement.SettlementId, settlement.TransferId, settlement.PaymentIntentId, settlement.SettlementReference, settlement.CorrelationId), cancellationToken);
        settlement.Status = providerResult.Success ? SettlementStatus.SETTLED : SettlementStatus.FAILED;
        settlement.ProviderReference = providerResult.ProviderReference;
        settlement.FailureCode = providerResult.FailureCode;
        settlement.FailureReason = providerResult.FailureReason;
        settlement.StartedAt = DateTimeOffset.UtcNow;
        settlement.SettledAt = providerResult.Success ? DateTimeOffset.UtcNow : null;
        settlement.FailedAt = providerResult.Success ? null : DateTimeOffset.UtcNow;
        settlement.Version += 1;
        await _settlementRepository.UpdateAsync(settlement, cancellationToken);

        return new CreateSettlementResponse(settlement.SettlementId, settlement.TransferId, settlement.Channel, settlement.Status, settlement.SettlementReference, settlement.SettledAt);
    }

    private static string BuildReference()
    {
        var random = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        return $"AFW-STL-{random}";
    }
}
