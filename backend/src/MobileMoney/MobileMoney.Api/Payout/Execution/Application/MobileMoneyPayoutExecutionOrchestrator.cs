using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Application;

public sealed class MobileMoneyPayoutExecutionOrchestrator
{
    private const string PreparedStatus = "Prepared";

    private readonly IMobileMoneyPayoutExecutionQuoteBindingReader _quoteReader;
    private readonly IMobileMoneyPayoutExecutionFundingBindingReader _fundingReader;
    private readonly IMobileMoneyPayoutExecutionBindingStore _bindingStore;
    private readonly IMobileMoneyPayoutClock _clock;

    public MobileMoneyPayoutExecutionOrchestrator(
        IMobileMoneyPayoutExecutionQuoteBindingReader quoteReader,
        IMobileMoneyPayoutExecutionFundingBindingReader fundingReader,
        IMobileMoneyPayoutExecutionBindingStore bindingStore,
        IMobileMoneyPayoutClock clock)
    {
        _quoteReader = quoteReader
            ?? throw new ArgumentNullException(nameof(quoteReader));
        _fundingReader = fundingReader
            ?? throw new ArgumentNullException(nameof(fundingReader));
        _bindingStore = bindingStore
            ?? throw new ArgumentNullException(nameof(bindingStore));
        _clock = clock
            ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<PrepareMobileMoneyPayoutExecutionResponse> PrepareAsync(
        PrepareMobileMoneyPayoutExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.QuoteId == Guid.Empty)
            throw new ArgumentException("Quote id cannot be empty.", nameof(request));
        if (request.FundingCorrelationId == Guid.Empty)
            throw new ArgumentException("Funding correlation id cannot be empty.", nameof(request));

        var idempotencyKey =
            MobileMoneyPayoutExecutionIdempotencyKey.Create(request.IdempotencyKey);

        var existing = await _bindingStore.FindByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Quote.QuoteId != request.QuoteId
                || existing.Funding.CorrelationId != request.FundingCorrelationId)
            {
                throw new MobileMoneyPayoutExecutionPreparationException(
                    MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
                    "The idempotency key is already bound to a different payout execution.");
            }

            return ToResponse(existing, isReplay: true);
        }

        var quote = await _quoteReader.FindByIdAsync(
            request.QuoteId,
            cancellationToken);

        if (quote is null)
        {
            throw new MobileMoneyPayoutExecutionPreparationException(
                MobileMoneyPayoutExecutionErrorCodes.QuoteNotFound,
                "The payout quote could not be found for execution.");
        }

        var funding = await _fundingReader.FindByCorrelationIdAsync(
            request.FundingCorrelationId,
            cancellationToken);

        if (funding is null)
        {
            throw new MobileMoneyPayoutExecutionPreparationException(
                MobileMoneyPayoutExecutionErrorCodes.FundingNotFound,
                "The payout funding plan could not be found for execution.");
        }

        var preparedAtUtc = _clock.UtcNow;
        if (preparedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Mobile Money payout execution clock must return UTC timestamps.");
        }

        MobileMoneyPayoutExecutionBinding binding;
        try
        {
            binding = MobileMoneyPayoutExecutionBinding.Create(
                quote,
                funding,
                idempotencyKey,
                preparedAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new MobileMoneyPayoutExecutionPreparationException(
                MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
                "The payout quote and funding plan cannot be prepared for execution.",
                exception);
        }

        await _bindingStore.SaveAsync(binding, cancellationToken);

        var authoritative = await _bindingStore.FindByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Prepared execution binding was not durably persisted.");

        if (authoritative.Quote.QuoteId != request.QuoteId
            || authoritative.Funding.CorrelationId != request.FundingCorrelationId)
        {
            throw new MobileMoneyPayoutExecutionPreparationException(
                MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to a different payout execution.");
        }

        return ToResponse(
            authoritative,
            isReplay: authoritative.BindingId != binding.BindingId);
    }

    private static PrepareMobileMoneyPayoutExecutionResponse ToResponse(
        MobileMoneyPayoutExecutionBinding binding,
        bool isReplay) =>
        new(
            binding.BindingId,
            binding.Quote.QuoteId,
            binding.Funding.CorrelationId,
            binding.IdempotencyKey.Value,
            PreparedStatus,
            isReplay,
            binding.CreatedAtUtc);
}
