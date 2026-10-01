namespace IdentityService.Api.Identity;

public sealed record CurrentIdentityReadModel(
    Guid UserId,
    string Identifier,
    DateTimeOffset CreatedAtUtc,
    string AfWalId);
