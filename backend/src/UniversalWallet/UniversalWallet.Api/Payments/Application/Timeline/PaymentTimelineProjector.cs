using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Settlements;
using UniversalWallet.Api.Payments.Domain.Timeline;
using UniversalWallet.Api.Payments.Domain.Transfers;

namespace UniversalWallet.Api.Payments.Application.Timeline;

public sealed class PaymentTimelineProjector
{
    private readonly IPaymentTimelineRepository _repository;
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentTransferRepository _transferRepository;
    private readonly ISettlementRepository _settlementRepository;

    public PaymentTimelineProjector(
        IPaymentTimelineRepository repository,
        IPaymentIntentRepository intentRepository,
        IPaymentTransferRepository transferRepository,
        ISettlementRepository settlementRepository)
    {
        _repository = repository;
        _intentRepository = intentRepository;
        _transferRepository = transferRepository;
        _settlementRepository = settlementRepository;
    }

    public async Task ProjectAsync(Guid paymentIntentId, CancellationToken cancellationToken = default)
    {
        var intent = await _intentRepository.GetAsync(paymentIntentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        var transfer = await _transferRepository.GetByIntentAsync(paymentIntentId, cancellationToken);
        var settlement = transfer is not null ? await _settlementRepository.GetByTransferIdAsync(transfer.TransferId, cancellationToken) : null;

        var item = new PaymentTimelineItem
        {
            OwnerAwidId = intent.PayerAwid,
            PaymentIntentId = intent.Id,
            TransferId = transfer?.TransferId,
            SettlementId = settlement?.SettlementId,
            Direction = PaymentTimelineDirection.Outgoing,
            Type = PaymentTimelineType.WalletTransfer,
            Status = MapStatus(intent, transfer, settlement),
            AmountMinor = intent.AmountMinor,
            CurrencyCode = intent.CurrencyCode,
            CounterpartyDisplayName = "Counterparty",
            CounterpartyAlias = "@counterparty",
            CounterpartyPublicAwid = intent.RecipientReference,
            Purpose = intent.Purpose.ToString(),
            Description = intent.Description,
            PublicReference = BuildReference(intent),
            OccurredAt = transfer?.ExecutedAt ?? intent.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            ReceiptAvailable = settlement?.Status == SettlementStatus.SETTLED,
            ProjectionVersion = 1
        };

        await _repository.AddOrUpdateAsync(item, cancellationToken);
    }

    private static PaymentTimelineStatus MapStatus(PaymentIntent intent, PaymentTransfer? transfer, Settlement? settlement)
    {
        if (settlement?.Status == SettlementStatus.SETTLED)
        {
            return PaymentTimelineStatus.Completed;
        }

        if (transfer?.Status == PaymentTransferStatus.Completed)
        {
            return PaymentTimelineStatus.Processing;
        }

        if (intent.Status == PaymentIntentStatus.Completed)
        {
            return PaymentTimelineStatus.Processing;
        }

        return PaymentTimelineStatus.Pending;
    }

    private static string BuildReference(PaymentIntent intent)
    {
        return $"AFW-PAY-{intent.Id.ToString("N")[..8].ToUpperInvariant()}";
    }
}
