using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class TokenizationService
{
    private readonly ITokenRepository _repository;
    private readonly TokenVault _vault;
    private readonly TokenValidator _validator;
    private readonly List<string> _telemetry = [];

    public TokenizationService(ITokenRepository repository, TokenVault vault, TokenValidator validator)
    {
        _repository = repository;
        _vault = vault;
        _validator = validator;
    }

    public async Task<CardToken?> CreateAsync(CardTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = new CardToken
        {
            CardId = request.CardId,
            OwnerAwidId = request.OwnerAwidId,
            WalletId = request.WalletId,
            Network = request.Network,
            TokenType = request.TokenType,
            TokenReference = _vault.CreateReference(),
            Status = "GENERATED",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Version = 1
        };

        var created = await _repository.CreateAsync(token, cancellationToken);
        _telemetry.Add("token-created");
        return created;
    }

    public async Task<CardToken?> ActivateAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null || token.Status is "REVOKED" or "EXPIRED") return null;
        token.Status = "ACTIVE";
        token.Version++;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-activated");
        return token;
    }

    public async Task<CardToken?> SuspendAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null || token.Status is not "ACTIVE") return null;
        token.Status = "SUSPENDED";
        token.Version++;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-suspended");
        return token;
    }

    public async Task<CardToken?> ResumeAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null || token.Status is not "SUSPENDED") return null;
        token.Status = "ACTIVE";
        token.Version++;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-resumed");
        return token;
    }

    public async Task<CardToken?> RotateAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null || token.Status is "REVOKED" or "EXPIRED") return null;
        token.TokenReference = _vault.CreateReference();
        token.Status = "ROTATED";
        token.Version++;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-rotated");
        return token;
    }

    public async Task<CardToken?> RevokeAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null) return null;
        token.Status = "REVOKED";
        token.Version++;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-revoked");
        return token;
    }

    public async Task<CardToken?> ValidateAsync(string reference, CancellationToken cancellationToken = default)
    {
        var token = await _repository.GetByReferenceAsync(reference, cancellationToken);
        if (token is null) return null;
        if (token.Status is "REVOKED" or "EXPIRED") return null;
        if (token.ExpiresAt is not null && token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            token.Status = "EXPIRED";
            token.Version++;
            await _repository.UpdateAsync(token, cancellationToken);
            _telemetry.Add("token-expired");
            return null;
        }

        if (!_validator.IsValid(token))
        {
            _telemetry.Add("token-invalid");
            return null;
        }

        token.LastUsedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(token, cancellationToken);
        _telemetry.Add("token-validated");
        return token;
    }

    public Task<IReadOnlyList<string>> GetAuditTrailAsync(string tokenId, CancellationToken cancellationToken = default)
        => _repository.GetAuditTrailAsync(tokenId, cancellationToken);

    public Task<IReadOnlyList<CardToken>> GetTokensForCardAsync(string cardId, CancellationToken cancellationToken = default)
        => _repository.GetTokensForCardAsync(cardId, cancellationToken);

    public int GetTelemetryCount() => _telemetry.Count;
}
