using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

public sealed class InMemoryTokenRepository : ITokenRepository
{
    private readonly List<CardToken> _tokens = [];
    private readonly Dictionary<string, List<string>> _audit = new(StringComparer.OrdinalIgnoreCase);

    public Task<CardToken> CreateAsync(CardToken token, CancellationToken cancellationToken = default)
    {
        _tokens.Add(token);
        _audit[token.TokenId] = ["CARD_TOKEN_CREATED"];
        return Task.FromResult(token);
    }

    public Task<CardToken?> GetByIdAsync(string tokenId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tokens.FirstOrDefault(t => t.TokenId.Equals(tokenId, StringComparison.OrdinalIgnoreCase)));

    public Task<CardToken?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        => Task.FromResult(_tokens.FirstOrDefault(t => t.TokenReference.Equals(reference, StringComparison.OrdinalIgnoreCase)));

    public Task<CardToken?> UpdateAsync(CardToken token, CancellationToken cancellationToken = default)
    {
        var existing = _tokens.FirstOrDefault(t => t.TokenId.Equals(token.TokenId, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return Task.FromResult<CardToken?>(null);

        var index = _tokens.IndexOf(existing);
        _tokens[index] = token;

        var statusEvent = token.Status switch
        {
            "ACTIVE" => "CARD_TOKEN_ACTIVATED",
            "SUSPENDED" => "CARD_TOKEN_SUSPENDED",
            "REVOKED" => "CARD_TOKEN_REVOKED",
            "ROTATED" => "CARD_TOKEN_ROTATED",
            "EXPIRED" => "CARD_TOKEN_EXPIRED",
            _ => "CARD_TOKEN_UPDATED"
        };

        if (_audit.ContainsKey(token.TokenId))
        {
            _audit[token.TokenId].Add(statusEvent);
        }
        else
        {
            _audit[token.TokenId] = [statusEvent];
        }

        return Task.FromResult<CardToken?>(token);
    }

    public Task<IReadOnlyList<CardToken>> GetTokensForCardAsync(string cardId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CardToken>>(_tokens.Where(t => t.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly());

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string tokenId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>((_audit.TryGetValue(tokenId, out var entries) ? entries : []).AsReadOnly());
}
