namespace IdentityService.Api.Identity;

public sealed record CurrentProfileReadModel(
    Guid UserId,
    string Identifier,
    DateTimeOffset CreatedAtUtc);
