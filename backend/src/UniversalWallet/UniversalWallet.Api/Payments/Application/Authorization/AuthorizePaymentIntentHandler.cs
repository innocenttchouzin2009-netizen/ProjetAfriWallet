using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Limits;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Risk;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Authorization;

public sealed record AuthorizePaymentIntentResponse(
    Guid AuthorizationId,
    PaymentAuthorizationDecision Decision,
    Guid ReservationId,
    long AuthorizedAmountMinor,
    string CurrencyCode,
    DateTimeOffset ExpiresAt,
    string NextAction);

public interface IPaymentAuthorizationRepository
{
    Task<PaymentAuthorization?> GetByIntentAsync(Guid intentId, CancellationToken cancellationToken = default);
    Task<PaymentAuthorization?> GetAsync(Guid authorizationId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentAuthorization authorization, CancellationToken cancellationToken = default);
}

public interface IPaymentReservationRepository
{
    Task<FundsReservation?> GetByIntentAsync(Guid intentId, CancellationToken cancellationToken = default);
    Task<FundsReservation?> GetAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task AddAsync(FundsReservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(FundsReservation reservation, CancellationToken cancellationToken = default);
}

public interface IPaymentLimitEngine
{
    PaymentLimits GetLimits(PaymentIntent intent, PaymentWalletSnapshot wallet, string deviceId, string sessionId);
    LimitValidationResult Validate(PaymentIntent intent, PaymentLimits limits);
}

public sealed class LimitValidationResult
{
    public bool IsValid { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
}

public interface IPaymentRiskEngine
{
    RiskAssessment Assess(PaymentIntent intent, PaymentWalletSnapshot wallet, string deviceId, string sessionId);
}

public sealed class AuthorizePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentAuthorizationRepository _authorizationRepository;
    private readonly IPaymentReservationRepository _reservationRepository;
    private readonly IPaymentWalletReader _walletReader;
    private readonly BalanceProjectionService _balanceProjectionService;
    private readonly IPaymentRiskEngine _riskEngine;
    private readonly IPaymentLimitEngine _limitEngine;

    public AuthorizePaymentIntentHandler(
        IPaymentIntentRepository intentRepository,
        IPaymentAuthorizationRepository authorizationRepository,
        IPaymentReservationRepository reservationRepository,
        IPaymentWalletReader walletReader,
        BalanceProjectionService balanceProjectionService,
        IPaymentRiskEngine riskEngine,
        IPaymentLimitEngine limitEngine)
    {
        _intentRepository = intentRepository;
        _authorizationRepository = authorizationRepository;
        _reservationRepository = reservationRepository;
        _walletReader = walletReader;
        _balanceProjectionService = balanceProjectionService;
        _riskEngine = riskEngine;
        _limitEngine = limitEngine;
    }

    public async Task<AuthorizePaymentIntentResponse> HandleAsync(Guid intentId, Guid payerAwid, string deviceId, string sessionId, CancellationToken cancellationToken = default)
    {
        var existingAuthorization = await _authorizationRepository.GetByIntentAsync(intentId, cancellationToken);
        if (existingAuthorization is not null)
        {
            return new AuthorizePaymentIntentResponse(
                existingAuthorization.Id,
                existingAuthorization.Decision,
                existingAuthorization.ReservationId ?? Guid.Empty,
                existingAuthorization.AuthorizedAmountMinor,
                existingAuthorization.CurrencyCode,
                existingAuthorization.ExpiresAt,
                "EXECUTE");
        }

        var intent = await _intentRepository.GetAsync(intentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        if (intent.Status == PaymentIntentStatus.Cancelled || intent.Status == PaymentIntentStatus.Expired)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_VALIDATABLE");
        }

        if (intent.Status == PaymentIntentStatus.Authorized || intent.Status == PaymentIntentStatus.Processing || intent.Status == PaymentIntentStatus.Completed)
        {
            throw new InvalidOperationException("PAYMENT_ALREADY_AUTHORIZED");
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

        var reservation = new FundsReservation
        {
            PaymentIntentId = intent.Id,
            WalletId = intent.SourceWalletId,
            AmountMinor = intent.AmountMinor,
            CurrencyCode = intent.CurrencyCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            Status = FundsReservationStatus.Active
        };

        await _reservationRepository.AddAsync(reservation, cancellationToken);

        intent.Status = PaymentIntentStatus.Authorized;
        await _intentRepository.AddAsync(intent, cancellationToken);

        var authorization = new PaymentAuthorization
        {
            PaymentIntentId = intent.Id,
            Decision = PaymentAuthorizationDecision.Approved,
            DecisionCode = "APPROVED",
            AuthorizedAmountMinor = intent.AmountMinor,
            CurrencyCode = intent.CurrencyCode,
            ReservationId = reservation.Id,
            RiskScore = risk.Score,
            RulesVersion = risk.RulesVersion,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        await _authorizationRepository.AddAsync(authorization, cancellationToken);

        return new AuthorizePaymentIntentResponse(
            authorization.Id,
            authorization.Decision,
            reservation.Id,
            authorization.AuthorizedAmountMinor,
            authorization.CurrencyCode,
            authorization.ExpiresAt,
            "EXECUTE");
    }
}
