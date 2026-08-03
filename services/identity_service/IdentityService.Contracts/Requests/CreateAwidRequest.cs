namespace IdentityService.Contracts.Requests;

public sealed class CreateAwidRequest
{
    public string Alias { get; init; } = string.Empty;
    public string PrivacyMode { get; init; } = "PRIVATE";
}
