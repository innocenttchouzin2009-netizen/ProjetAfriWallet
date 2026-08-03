using System.Security.Claims;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Services;
using IdentityService.Contracts.Requests;
using IdentityService.Contracts.Responses;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Handlers;

public sealed class ChangeAliasHandler
{
    private readonly IAwidRepository _awidRepository;
    private readonly IAuthenticationEventRepository _auditRepository;
    private readonly AwidPolicy _policy;

    public ChangeAliasHandler(IAwidRepository awidRepository, IAuthenticationEventRepository auditRepository, AwidPolicy policy)
    {
        _awidRepository = awidRepository;
        _auditRepository = auditRepository;
        _policy = policy;
    }

    public async Task<AwidResponse> HandleAsync(ChangeAliasRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new AwidResponse { Success = false, ErrorCode = "UNAUTHORIZED", Message = "Authenticated user required" };
        }

        var alias = AwidAliasRules.Normalize(request.Alias);
        if (!AwidAliasRules.IsValidCanonical(alias))
        {
            return new AwidResponse { Success = false, ErrorCode = "ALIAS_INVALID", Message = "Alias is invalid" };
        }

        if (_policy.ProtectedAliases.Contains(alias))
        {
            return new AwidResponse { Success = false, ErrorCode = "ALIAS_PROTECTED", Message = "Alias is protected" };
        }

        var changeResult = await _awidRepository.TryChangeAliasAsync(
            userId,
            alias,
            DateTimeOffset.UtcNow,
            _policy.AliasChangeCooldown,
            _policy.PreviousAliasReservation,
            cancellationToken);

        if (!changeResult.Success)
        {
            return changeResult.FailureReason switch
            {
                AwidAliasChangeFailureReason.AwidNotFound => new AwidResponse { Success = false, ErrorCode = "AWID_NOT_FOUND", Message = "AWID not found" },
                AwidAliasChangeFailureReason.CooldownNotReached => new AwidResponse { Success = false, ErrorCode = "ALIAS_CHANGE_TOO_SOON", Message = "Alias change too soon" },
                _ => new AwidResponse { Success = false, ErrorCode = "ALIAS_ALREADY_TAKEN", Message = "Alias already taken" }
            };
        }

        var awid = changeResult.Awid!;
        await _auditRepository.SaveAsync(new AuthenticationEvent
        {
            UserId = userId,
            EventType = "ALIAS_CHANGED",
            Details = $"Changed alias to {awid.AliasDisplay}"
        }, cancellationToken);

        return new AwidResponse
        {
            Success = true,
            PublicAwid = awid.PublicAwid,
            Alias = awid.AliasDisplay,
            Status = awid.Status.ToString().ToUpperInvariant(),
            Message = "Alias changed successfully"
        };
    }
}
