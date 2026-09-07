namespace IdentityService.Api.Auth.Domain;

public enum RefreshRotationStatus
{
    Succeeded,
    NotFound,
    Revoked,
    Expired,
    Reused
}
