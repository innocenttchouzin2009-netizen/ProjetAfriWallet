using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class VirtualCardService
{
    private readonly IVirtualCardRepository _repository;

    public VirtualCardService(IVirtualCardRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<VirtualCard>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<VirtualCard?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(cardId, cancellationToken);

    public async Task<VirtualCard> CreateAsync(VirtualCard request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.FindByTokenAsync(request.CardToken, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var card = new VirtualCard
        {
            VirtualCardId = string.IsNullOrWhiteSpace(request.VirtualCardId) ? Guid.NewGuid().ToString("N") : request.VirtualCardId,
            CardProgramId = request.CardProgramId,
            OwnerAwidId = request.OwnerAwidId,
            WalletId = request.WalletId,
            CardholderName = request.CardholderName,
            CardToken = string.IsNullOrWhiteSpace(request.CardToken) ? Guid.NewGuid().ToString("N") : request.CardToken,
            MaskedPan = MaskedPanFor(request.CardholderName),
            LastFour = LastFourFor(request.CardToken),
            ExpiryMonth = request.ExpiryMonth > 0 ? request.ExpiryMonth : 12,
            ExpiryYear = request.ExpiryYear > 0 ? request.ExpiryYear : DateTime.UtcNow.Year + 3,
            Status = "ISSUED",
            SpendingLimitMinor = request.SpendingLimitMinor > 0 ? request.SpendingLimitMinor : 500_000,
            DailyLimitMinor = request.DailyLimitMinor > 0 ? request.DailyLimitMinor : 1_000_000,
            MonthlyLimitMinor = request.MonthlyLimitMinor > 0 ? request.MonthlyLimitMinor : 5_000_000,
            BaseCurrency = string.IsNullOrWhiteSpace(request.BaseCurrency) ? "XAF" : request.BaseCurrency,
            AllowedCurrencies = request.AllowedCurrencies?.Count > 0 ? request.AllowedCurrencies : ["XAF"],
            EcommerceEnabled = request.EcommerceEnabled,
            ContactlessEnabled = request.ContactlessEnabled,
            InternationalEnabled = request.InternationalEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        return await _repository.CreateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> ActivateAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is "ACTIVE" or "CLOSED" or "REPLACED") return card;
        if (card.Status is not "ISSUED") return null;

        card.Status = "ACTIVE";
        card.ActivatedAt = DateTimeOffset.UtcNow;
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> FreezeAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is "FROZEN" or "CLOSED" or "REPLACED") return card;
        if (card.Status is not "ACTIVE") return null;

        card.Status = "FROZEN";
        card.FrozenAt = DateTimeOffset.UtcNow;
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> UnfreezeAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is not "FROZEN") return null;

        card.Status = "ACTIVE";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> CloseAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is "CLOSED" or "REPLACED") return card;

        card.Status = "CLOSED";
        card.ClosedAt = DateTimeOffset.UtcNow;
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> UpdateControlsAsync(string cardId, bool ecommerceEnabled, bool contactlessEnabled, bool internationalEnabled, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is "CLOSED" or "REPLACED") return null;

        card.EcommerceEnabled = ecommerceEnabled;
        card.ContactlessEnabled = contactlessEnabled;
        card.InternationalEnabled = internationalEnabled;
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<VirtualCard?> UpdateLimitsAsync(string cardId, long spendingLimitMinor, long dailyLimitMinor, long monthlyLimitMinor, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null) return null;
        if (card.Status is "CLOSED" or "REPLACED") return null;

        card.SpendingLimitMinor = spendingLimitMinor;
        card.DailyLimitMinor = dailyLimitMinor;
        card.MonthlyLimitMinor = monthlyLimitMinor;
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetAuditTrailAsync(cardId, cancellationToken);

    private static string MaskedPanFor(string cardholderName)
    {
        return $"**** **** **** {cardholderName.GetHashCode().ToString("X").PadLeft(4, '0')}";
    }

    private static string LastFourFor(string cardToken)
    {
        var normalized = cardToken.Replace("-", string.Empty);
        return normalized.Length >= 4 ? normalized[^4..] : normalized;
    }
}
