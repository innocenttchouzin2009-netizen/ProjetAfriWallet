using IdentityService.Application.Abstractions;
using IdentityService.Application.Services;
using IdentityService.Contracts.Requests;
using IdentityService.Contracts.Responses;

namespace IdentityService.Application.Handlers;

public sealed class CheckAliasAvailabilityHandler
{
    private readonly IAwidRepository _awidRepository;

    public CheckAliasAvailabilityHandler(IAwidRepository awidRepository)
    {
        _awidRepository = awidRepository;
    }

    public async Task<AliasAvailabilityResponse> HandleAsync(GetAliasAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var alias = AwidAliasRules.Normalize(request.Alias);
        if (!AwidAliasRules.IsValidCanonical(alias))
        {
            return new AliasAvailabilityResponse
            {
                Success = false,
                Alias = request.Alias,
                Available = false,
                Suggestions = Array.Empty<string>()
            };
        }

        var taken = !await _awidRepository.IsAliasAvailableAsync(alias, cancellationToken);
        var suggestions = new List<string>();

        if (taken)
        {
            suggestions.Add(alias + "237");
            suggestions.Add(alias + "_t");
            suggestions.Add(alias + "84");
        }

        return new AliasAvailabilityResponse
        {
            Success = true,
            Alias = alias,
            Available = !taken,
            Suggestions = suggestions
        };
    }
}
