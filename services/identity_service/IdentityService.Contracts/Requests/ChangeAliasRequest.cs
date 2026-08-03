namespace IdentityService.Contracts.Requests;

public sealed class ChangeAliasRequest
{
    public string Alias { get; init; } = string.Empty;
}
