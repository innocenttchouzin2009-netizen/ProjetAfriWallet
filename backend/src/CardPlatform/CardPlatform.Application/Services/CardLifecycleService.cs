using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class CardLifecycleService
{
    private readonly ICardLifecycleRepository _repository;

    public CardLifecycleService(ICardLifecycleRepository repository)
    {
        _repository = repository;
    }

    public async Task<CardLifecycle?> IssueAsync(CardLifecycleRequest request, CancellationToken cancellationToken = default)
    {
        var card = new CardLifecycle
        {
            CardId = request.CardId,
            OwnerAwidId = request.OwnerAwidId,
            WalletId = request.WalletId,
            Status = "ISSUED",
            LastTransition = "CARD_ISSUED"
        };

        return await _repository.CreateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> ActivateAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "ISSUED") return null;
        card.Status = "ACTIVE";
        card.LastTransition = "CARD_ACTIVATED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> FreezeAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "ACTIVE") return null;
        card.Status = "FROZEN";
        card.LastTransition = "CARD_FROZEN";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> UnfreezeAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "FROZEN") return null;
        card.Status = "ACTIVE";
        card.LastTransition = "CARD_UNFROZEN";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> SuspendAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "ACTIVE") return null;
        card.Status = "SUSPENDED";
        card.LastTransition = "CARD_SUSPENDED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> ResumeAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "SUSPENDED") return null;
        card.Status = "ACTIVE";
        card.LastTransition = "CARD_RESUMED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> ReplaceAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status is not ("ACTIVE" or "SUSPENDED")) return null;
        card.Status = "REPLACED";
        card.LastTransition = "CARD_REPLACED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> ExpireAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status is not ("ACTIVE" or "SUSPENDED" or "FROZEN" or "REPLACED")) return null;
        card.Status = "EXPIRED";
        card.LastTransition = "CARD_EXPIRED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public async Task<CardLifecycle?> CloseAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var card = await _repository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.Status != "EXPIRED") return null;
        card.Status = "CLOSED";
        card.LastTransition = "CARD_CLOSED";
        card.Version++;
        return await _repository.UpdateAsync(card, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetAuditTrailAsync(cardId, cancellationToken);

    public Task<IReadOnlyList<string>> GetTimelineAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetTimelineAsync(cardId, cancellationToken);

    public Task<CardLifecycle?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(cardId, cancellationToken);
}
