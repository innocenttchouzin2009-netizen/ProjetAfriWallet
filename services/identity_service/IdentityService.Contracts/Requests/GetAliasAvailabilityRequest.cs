namespace IdentityService.Contracts.Requests;

public sealed class GetAliasAvailabilityRequest
{
    public string Alias { get; init; } = string.Empty;
}
