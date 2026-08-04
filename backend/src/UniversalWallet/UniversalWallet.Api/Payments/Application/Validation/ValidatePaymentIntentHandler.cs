using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Limits;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Risk;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Validation;

public sealed record ValidatePaymentIntentRequest(Guid IntentId, string DeviceId, string SessionId);

public sealed record ValidatePaymentIntentResponse(
    Guid IntentId,
    PaymentIntentStatus Status,
    string ValidationResult,
    string NextAction);

public interface IPaymentValidationService
{
    Task<ValidationOutcome> ValidateAsync(Guid intentId, Guid payerAwid, string deviceId, string sessionId, CancellationToken cancellationToken = default);
}

public sealed class ValidationOutcome
{
    public PaymentIntent? Intent { get; init; }
    public PaymentAuthorization? Authorization { get; init; }
    public FundsReservation? Reservation { get; init; }
    public PaymentLimits? Limits { get; init; }
    public RiskAssessment? RiskAssessment { get; init; }
    public string? ErrorCode { get; init; }
    public bool IsValid { get; init; }
    public string ValidationResult { get; init; } = string.Empty;
    public string NextAction { get; init; } = string.Empty;
}

public sealed class ValidatePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentWalletReader _walletReader;
    private readonly BalanceProjectionService _balanceProjectionService;
    private readonly IPaymentAuthorizationRepository _authorizationRepository;
    private readonly IPaymentReservationRepository _reservationRepository;
    private readonly IPaymentRiskEngine _riskEngine;
    private readonly IPaymentLimitEngine _limitEngine;

    public ValidatePaymentIntentHandler(
        IPaymentIntentRepository intentRepository,
        IPaymentWalletReader walletReader,
        BalanceProjectionService balanceProjectionService,
        IPaymentAuthorizationRepository authorizationRepository,
        IPaymentReservationRepository reservationRepository,
        IPaymentRiskEngine riskEngine,
        IPaymentLimitEngine limitEngine)
    {
        _intentRepository = intentRepository;
        _walletReader = walletReader;
        _balanceProjectionService = balanceProjectionService;
        _authorizationRepository = authorizationRepository;
        _reservationRepository = reservationRepository;
        _riskEngine = riskEngine;
        _limitEngine = limitEngine;
    }

    public async Task<ValidatePaymentIntentResponse> HandleAsync(Guid intentId, Guid payerAwid, string deviceId, string sessionId, CancellationToken cancellationToken = default)
    {
        var intent = await _intentRepository.GetAsync(intentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        if (intent.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            intent.MarkExpired();
            await _intentRepository.AddAsync(intent, cancellationToken);
            throw new InvalidOperationException("PAYMENT_INTENT_EXPIRED");
        }

        if (intent.Status != PaymentIntentStatus.Created)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_VALIDATABLE");
        }

        var wallet = await _walletReader.GetAsync(intent.SourceWalletId, cancellationToken);
        if (wallet is null)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_NOT_FOUND");
        }

        if (wallet.AwidId != payerAwid)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_FORBIDDEN");
        }

        if (wallet.Status != WalletStatus.Active)
        {
            throw new InvalidOperationException("PAYMENT_WALLET_NOT_ACTIVE");
        }

        if (!string.Equals(wallet.Currency, intent.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PAYMENT_CURRENCY_MISMATCH");
        }

        var projectionState = _balanceProjectionService.GetProjectionState(intent.SourceWalletId);
        if (!projectionState.IsUpToDate)
        {
            throw new InvalidOperationException("BALANCE_PROJECTION_STALE");
        }

        if (projectionState.Projection.AvailableBalance < (decimal)intent.AmountMinor / 100m)
        {
            throw new InvalidOperationException("INSUFFICIENT_AVAILABLE_BALANCE");
        }

        var limits = _limitEngine.GetLimits(intent, wallet, deviceId, sessionId);
        var limitResult = _limitEngine.Validate(intent, limits);
        if (!limitResult.IsValid)
        {
            throw new InvalidOperationException(limitResult.ErrorCode);
        }

        var risk = _riskEngine.Assess(intent, wallet, deviceId, sessionId);
        if (risk.RecommendedAction == RecommendedRiskAction.Block)
        {
            throw new InvalidOperationException("PAYMENT_AUTHORIZATION_DECLINED");
        }

        if (risk.RecommendedAction == RecommendedRiskAction.Review)
        {
            throw new InvalidOperationException("PAYMENT_REVIEW_REQUIRED");
        }

        if (risk.RecommendedAction == RecommendedRiskAction.StepUp)
        {
            throw new InvalidOperationException("PAYMENT_STEP_UP_REQUIRED");
        }

        intent.Status = PaymentIntentStatus.Validated;
        await _intentRepository.AddAsync(intent, cancellationToken);

        return new ValidatePaymentIntentResponse(intent.Id, intent.Status, "PASSED", "AUTHORIZE");
    }
}
