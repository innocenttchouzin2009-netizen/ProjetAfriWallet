namespace IdentityService.Api.Engine;

public sealed class CreateTempQrRequest
{
    public QrType Type { get; init; } = QrType.Contact;
    public string Purpose { get; init; } = "RECEIVE_PAYMENT";
    public int ExpiresInMinutes { get; init; } = 10;
    public int MaxUses { get; init; } = 1;
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
}

public sealed class CreatePaymentQrRequest
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}

public sealed class ResolveQrRequest
{
    public string Token { get; init; } = string.Empty;
    public QrType? ExpectedType { get; init; }
}

public sealed class QrTokenResponse
{
    public Guid Id { get; init; }
    public string Token { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
    public int MaxUses { get; init; }
    public int UseCount { get; init; }
}

public sealed class MeCardResponse
{
    public string Alias { get; init; } = string.Empty;
    public string PublicAwid { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string PrivacyMode { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public IReadOnlyList<string> VerificationBadges { get; init; } = Array.Empty<string>();
    public string? BusinessName { get; init; }
    public string? AssociationName { get; init; }
    public string? BusinessHours { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
