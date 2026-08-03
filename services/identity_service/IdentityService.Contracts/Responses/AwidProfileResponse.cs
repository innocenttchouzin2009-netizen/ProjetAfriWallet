namespace IdentityService.Contracts.Responses;

public sealed class AwidProfileResponse
{
    public bool Success { get; init; }
    public string PublicAwid { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public string PrivacyMode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
