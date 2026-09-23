namespace IdentityService.Api.MerchantTeamAccess;

public sealed record AddMerchantTeamMemberRequest(string AfWalId, string Role);
public sealed record ChangeMerchantTeamMemberRoleRequest(string Role);

public sealed record MerchantTeamMemberResponse(
    Guid MemberId,
    string MerchantId,
    string AfWalId,
    string Role,
    string Status,
    IReadOnlyCollection<string> Permissions,
    bool MoneyMovementAllowed,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MerchantTeamErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class MerchantTeamErrorCode
{
    public const string Forbidden = "MERCHANT_TEAM_FORBIDDEN";
    public const string NotFound = "MERCHANT_TEAM_NOT_FOUND";
    public const string ValidationError = "MERCHANT_TEAM_VALIDATION_ERROR";
    public const string Conflict = "MERCHANT_TEAM_CONFLICT";
}
