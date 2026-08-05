using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class CardAuthorizationService
{
    private readonly ICardProgramRepository _cardProgramRepository;
    private readonly IVirtualCardRepository _cardRepository;
    private readonly ICardAuthorizationRepository _authorizationRepository;

    private static readonly Dictionary<string, long> WalletBalances = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wallet-001"] = 1_000_000,
        ["wallet-002"] = 1_000_000,
        ["wallet-003"] = 1_000_000,
        ["wallet-004"] = 1_000_000,
        ["wallet-005"] = 1_000_000
    };

    public CardAuthorizationService(
        ICardProgramRepository cardProgramRepository,
        IVirtualCardRepository cardRepository,
        ICardAuthorizationRepository authorizationRepository)
    {
        _cardProgramRepository = cardProgramRepository;
        _cardRepository = cardRepository;
        _authorizationRepository = authorizationRepository;
    }

    public async Task<CardAuthorization> AuthorizeAsync(CardAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var authorization = new CardAuthorization
        {
            CardId = request.CardId,
            WalletId = request.WalletId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode,
            MerchantCategoryCode = request.MerchantCategoryCode,
            MerchantCountry = request.MerchantCountry,
            Channel = request.Channel,
            CorrelationId = Guid.NewGuid().ToString("N"),
            TraceId = Guid.NewGuid().ToString("N")
        };

        var card = await _cardRepository.GetByIdAsync(request.CardId, cancellationToken);
        if (card is null)
        {
            authorization.Decision = "DECLINED";
            authorization.ReasonCode = "CARD_NOT_FOUND";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (!string.Equals(card.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            authorization.Decision = card.Status.Equals("FROZEN", StringComparison.OrdinalIgnoreCase) ? "CARD_FROZEN" : "CARD_CLOSED";
            authorization.ReasonCode = card.Status.ToUpperInvariant();
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        var program = await _cardProgramRepository.GetByIdAsync(card.CardProgramId, cancellationToken);
        if (program is null || !string.Equals(program.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            authorization.Decision = "DECLINED";
            authorization.ReasonCode = "PROGRAM_INVALID";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.Channel.Equals("online", StringComparison.OrdinalIgnoreCase) && !card.EcommerceEnabled)
        {
            authorization.Decision = "CONTROL_BLOCKED";
            authorization.ReasonCode = "ECOMMERCE_DISABLED";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.AmountMinor > card.SpendingLimitMinor || request.AmountMinor > program.Limits.SingleTransactionLimitMinor)
        {
            authorization.Decision = "LIMIT_EXCEEDED";
            authorization.ReasonCode = "TRANSACTION_LIMIT";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.AmountMinor > card.DailyLimitMinor || request.AmountMinor > program.Limits.DailyLimitMinor)
        {
            authorization.Decision = "LIMIT_EXCEEDED";
            authorization.ReasonCode = "DAILY_LIMIT";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.AmountMinor > card.MonthlyLimitMinor || request.AmountMinor > program.Limits.MonthlyLimitMinor)
        {
            authorization.Decision = "LIMIT_EXCEEDED";
            authorization.ReasonCode = "MONTHLY_LIMIT";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (!card.AllowedCurrencies.Contains(request.CurrencyCode, StringComparer.OrdinalIgnoreCase) && !card.AllowedCurrencies.Contains(program.BaseCurrency, StringComparer.OrdinalIgnoreCase))
        {
            authorization.Decision = "DECLINED";
            authorization.ReasonCode = "CURRENCY_NOT_SUPPORTED";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        var availableBalance = WalletBalances.TryGetValue(request.WalletId, out var balance) ? balance : 1_000_000;
        if (request.AmountMinor > availableBalance)
        {
            authorization.Decision = "INSUFFICIENT_FUNDS";
            authorization.ReasonCode = "BALANCE_LOW";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.Metadata.TryGetValue("risk_score", out var rawRiskScore) && rawRiskScore is not null && Convert.ToInt32(rawRiskScore) >= 80)
        {
            authorization.Decision = "FRAUD_SUSPECTED";
            authorization.ReasonCode = "RISK_HIGH";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        if (request.Metadata.TryGetValue("risk_score", out var rawRisk) && rawRisk is not null && Convert.ToInt32(rawRisk) >= 60)
        {
            authorization.Decision = "MANUAL_REVIEW";
            authorization.ReasonCode = "RISK_MEDIUM";
            authorization.ApprovedAmountMinor = 0;
            authorization.DurationMs = stopwatch.ElapsedMilliseconds;
            return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
        }

        authorization.Decision = "AUTHORIZED";
        authorization.ReasonCode = "APPROVED";
        authorization.ApprovedAmountMinor = request.AmountMinor;
        authorization.DurationMs = stopwatch.ElapsedMilliseconds;
        return await _authorizationRepository.CreateAsync(authorization, cancellationToken);
    }

    public Task<CardAuthorization?> GetByIdAsync(string authorizationId, CancellationToken cancellationToken = default)
        => _authorizationRepository.GetByIdAsync(authorizationId, cancellationToken);

    public async Task<CardAuthorization?> ReverseAsync(CardAuthorizationReverseRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationRepository.GetByIdAsync(request.AuthorizationId, cancellationToken);
        if (authorization is null) return null;

        authorization.Decision = "REVERSED";
        authorization.ReasonCode = request.ReasonCode;
        authorization.Version++;
        return authorization;
    }
}
