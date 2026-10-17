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
    private readonly IMobileMoneyPayoutExecutionOwnershipReader? _ownershipReader;

    public MobileMoneyPayoutExecutionOrchestrator(
        IMobileMoneyPayoutExecutionQuoteBindingReader quoteReader,
        IMobileMoneyPayoutExecutionFundingBindingReader fundingReader,
        IMobileMoneyPayoutExecutionBindingStore bindingStore,
        IMobileMoneyPayoutClock clock)
        : this(quoteReader, fundingReader, bindingStore, clock, null)
    {
    }

    public MobileMoneyPayoutExecutionOrchestrator(
        IMobileMoneyPayoutExecutionQuoteBindingReader quoteReader,
        IMobileMoneyPayoutExecutionFundingBindingReader fundingReader,
        IMobileMoneyPayoutExecutionBindingStore bindingStore,
        IMobileMoneyPayoutClock clock,
        IMobileMoneyPayoutExecutionOwnershipReader? ownershipReader)
    {
        _quoteReader = quoteReader
            ?? throw new ArgumentNullException(nameof(quoteReader));
        _fundingReader = fundingReader
            ?? throw new ArgumentNullException(nameof(fundingReader));
        _bindingStore = bindingStore
            ?? throw new ArgumentNullException(nameof(bindingStore));
        _clock = clock
            ?? throw new ArgumentNullException(nameof(clock));
        _ownershipReader = ownershipReader;
    }

    public Task<PrepareMobileMoneyPayoutExecutionResponse> PrepareAsync(
        PrepareMobileMoneyPayoutExecutionRequest request,
        CancellationToken cancellationToken = default) =>
        PrepareCoreAsync(request, Guid.Empty, cancellationToken);

    public Task<PrepareMobileMoneyPayoutExecutionResponse> PrepareAsync(
        PrepareMobileMoneyPayoutExecutionRequest request,
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        return PrepareCoreAsync(request, ownerUserId, cancellationToken);
    }

    private async Task<PrepareMobileMoneyPayoutExecutionResponse> PrepareCoreAsync(
        PrepareMobileMoneyPayoutExecutionRequest request,
        Guid ownerUserId,
        CancellationToken cancellationToken)
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
            if (ownerUserId != Guid.Empty)
            {
                var bindingOwner = await _bindingStore.FindOwnerUserIdAsync(
                    idempotencyKey,
                    cancellationToken);

                if (bindingOwner is null || bindingOwner != ownerUserId)
                {
                    throw new MobileMoneyPayoutExecutionPreparationException(
                        MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
                        "The payout execution binding is not available for the authenticated user.");
                }
            }

            if (existing.Quote.QuoteId != request.QuoteId
                || existing.Funding.CorrelationId != request.FundingCorrelationId)
            {
                throw new MobileMoneyPayoutExecutionPreparationException(
                    MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
                    "The idempotency key is already bound to a different payout execution.");
            }

            return ToResponse(existing, isReplay: true);
        }

        if (ownerUserId != Guid.Empty)
        {
            var ownershipReader = _ownershipReader
                ?? throw new MobileMoneyPayoutExecutionPreparationException(
                    MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
                    "Execution ownership validation is unavailable.");

            var quoteOwner = await ownershipReader.FindQuoteOwnerUserIdAsync(
                request.QuoteId,
                cancellationToken);
            var fundingOwner = await ownershipReader.FindFundingOwnerUserIdAsync(
                request.FundingCorrelationId,
                cancellationToken);

            if (quoteOwner is null
                || fundingOwner is null
                || quoteOwner != ownerUserId
                || fundingOwner != ownerUserId
                || quoteOwner != fundingOwner)
            {
                throw new MobileMoneyPayoutExecutionPreparationException(
                    MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
                    "The payout execution material is not available for the authenticated user.");
            }
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

        if (ownerUserId == Guid.Empty)
            await _bindingStore.SaveAsync(binding, cancellationToken);
        else
            await _bindingStore.SaveOwnedAsync(ownerUserId, binding, cancellationToken);

        var authoritative = await _bindingStore.FindByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Prepared execution binding was not durably persisted.");

        if (ownerUserId != Guid.Empty)
        {
            var authoritativeOwner = await _bindingStore.FindOwnerUserIdAsync(
                idempotencyKey,
                cancellationToken);

            if (authoritativeOwner is null || authoritativeOwner != ownerUserId)
            {
                throw new MobileMoneyPayoutExecutionPreparationException(
                    MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
                    "The payout execution binding is not available for the authenticated user.");
            }
        }

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
