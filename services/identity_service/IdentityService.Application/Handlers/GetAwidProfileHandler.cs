using System.Security.Claims;
using IdentityService.Application.Abstractions;
using IdentityService.Contracts.Responses;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Handlers;

public sealed class GetAwidProfileHandler
{
    private readonly IAwidRepository _awidRepository;

    public GetAwidProfileHandler(IAwidRepository awidRepository)
    {
        _awidRepository = awidRepository;
    }

    public async Task<AwidProfileResponse> HandleAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new AwidProfileResponse { Success = false, Status = "UNAUTHORIZED" };
        }

        var awid = await _awidRepository.GetBySubjectIdAsync(userId, cancellationToken);
        if (awid is null)
        {
            return new AwidProfileResponse { Success = false, Status = "AWID_NOT_FOUND" };
        }

        return new AwidProfileResponse
        {
            Success = true,
            PublicAwid = awid.PublicAwid,
            Alias = awid.AliasDisplay,
            PrivacyMode = awid.PrivacyMode.ToString().ToUpperInvariant(),
            Status = awid.Status.ToString().ToUpperInvariant()
        };
    }
}
