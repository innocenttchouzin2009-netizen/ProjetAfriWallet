using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

public sealed class InMemoryCardRepository : ICardLifecycleRepository
{
    private readonly Dictionary<string, CardLifecycle> _cards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _audit = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _timeline = new(StringComparer.OrdinalIgnoreCase);

    public Task<CardLifecycle> CreateAsync(CardLifecycle card, CancellationToken cancellationToken = default)
    {
        _cards[card.CardId] = card;
        _audit[card.CardId] = [card.LastTransition ?? "CARD_ISSUED"];
        _timeline[card.CardId] = [$"{DateTimeOffset.UtcNow:HH:mm} Card Issued"];
        return Task.FromResult(card);
    }

    public Task<CardLifecycle?> GetByIdAsync(string cardId, CancellationToken cancellationToken = default)
        => Task.FromResult(_cards.TryGetValue(cardId, out var card) ? card : null);

    public Task<CardLifecycle?> UpdateAsync(CardLifecycle card, CancellationToken cancellationToken = default)
    {
        if (!_cards.ContainsKey(card.CardId)) return Task.FromResult<CardLifecycle?>(null);

        _cards[card.CardId] = card;
        _audit[card.CardId].Add(card.LastTransition ?? "CARD_UPDATED");
        _timeline[card.CardId].Add($"{DateTimeOffset.UtcNow:HH:mm} {card.LastTransition?.Replace("CARD_", string.Empty).Replace("_", " ").ToTitleCase()}");
        return Task.FromResult<CardLifecycle?>(card);
    }

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string cardId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>((_audit.TryGetValue(cardId, out var entries) ? entries : []).AsReadOnly());

    public Task<IReadOnlyList<string>> GetTimelineAsync(string cardId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>((_timeline.TryGetValue(cardId, out var entries) ? entries : []).AsReadOnly());
}

internal static class StringExtensions
{
    public static string ToTitleCase(this string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }
}
