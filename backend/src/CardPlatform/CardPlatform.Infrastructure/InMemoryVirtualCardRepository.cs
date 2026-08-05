using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

public sealed class InMemoryVirtualCardRepository : IVirtualCardRepository
{
    private readonly List<VirtualCard> _cards = [];
    private readonly Dictionary<string, List<string>> _audit = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<VirtualCard>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<VirtualCard>>(_cards.AsReadOnly());

    public Task<VirtualCard?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default)
        => Task.FromResult(_cards.FirstOrDefault(c => c.VirtualCardId.Equals(cardId, StringComparison.OrdinalIgnoreCase)));

    public Task<VirtualCard?> FindByTokenAsync(string cardToken, CancellationToken cancellationToken = default)
        => Task.FromResult(_cards.FirstOrDefault(c => c.CardToken.Equals(cardToken, StringComparison.OrdinalIgnoreCase)));

    public Task<VirtualCard> CreateAsync(VirtualCard card, CancellationToken cancellationToken = default)
    {
        _cards.Add(card);
        _audit[card.VirtualCardId] = ["created"];
        return Task.FromResult(card);
    }

    public Task<VirtualCard?> UpdateAsync(VirtualCard card, CancellationToken cancellationToken = default)
    {
        var existing = _cards.FirstOrDefault(c => c.VirtualCardId.Equals(card.VirtualCardId, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return Task.FromResult<VirtualCard?>(null);

        var index = _cards.IndexOf(existing);
        _cards[index] = card;
        if (_audit.ContainsKey(card.VirtualCardId))
        {
            _audit[card.VirtualCardId].Add($"status:{card.Status}");
        }
        else
        {
            _audit[card.VirtualCardId] = ["updated"];
        }

        return Task.FromResult<VirtualCard?>(card);
    }

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>((_audit.TryGetValue(cardId, out var entries) ? entries : []).AsReadOnly());
    }
}
