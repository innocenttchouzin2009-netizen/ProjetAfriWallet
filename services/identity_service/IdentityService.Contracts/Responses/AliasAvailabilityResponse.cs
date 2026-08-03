namespace IdentityService.Contracts.Responses;

public sealed class AliasAvailabilityResponse
{
    public bool Success { get; init; }
    public string Alias { get; init; } = string.Empty;
    public bool Available { get; init; }
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}
