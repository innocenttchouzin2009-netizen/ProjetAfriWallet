namespace IdentityService.Contracts.Responses;

public sealed class AwidPublicProfileResponse
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string PublicAwid { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PrivacyMode { get; init; } = string.Empty;
}
