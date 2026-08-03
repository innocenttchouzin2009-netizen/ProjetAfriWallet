using System.Security.Claims;
using System.Security.Cryptography;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Services;
using IdentityService.Contracts.Requests;
using IdentityService.Contracts.Responses;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Handlers;

public sealed class CreateAwidHandler
{
    private const string PublicAwidAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private readonly IAwidRepository _awidRepository;
    private readonly IAuthenticationEventRepository _auditRepository;
    private readonly AwidPolicy _policy;

    public CreateAwidHandler(IAwidRepository awidRepository, IAuthenticationEventRepository auditRepository, AwidPolicy policy)
    {
        _awidRepository = awidRepository;
        _auditRepository = auditRepository;
        _policy = policy;
    }

    public async Task<AwidResponse> HandleAsync(CreateAwidRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
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

        for (var attempt = 0; attempt < _policy.MaxCreateAttempts; attempt++)
        {
            var awid = new Awid
            {
                SubjectId = userId,
                PublicAwid = GeneratePublicAwid(),
                AliasCanonical = alias,
                AliasDisplay = $"@{alias}",
                Status = AwidStatus.Active,
                PrivacyMode = ParsePrivacyMode(request.PrivacyMode),
                IssuedMarket = _policy.IssuedMarket,
                ActivatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Version = 1
            };

            var createResult = await _awidRepository.TryCreateAsync(awid, cancellationToken);
            if (!createResult.Success)
            {
                if (createResult.FailureReason == AwidCreateFailureReason.PublicAwidAlreadyExists)
                {
                    continue;
                }

                if (createResult.FailureReason == AwidCreateFailureReason.SubjectAlreadyExists)
                {
                    return new AwidResponse { Success = false, ErrorCode = "AWID_ALREADY_EXISTS", Message = "AWID already exists" };
                }

                return new AwidResponse { Success = false, ErrorCode = "ALIAS_ALREADY_TAKEN", Message = "Alias already taken" };
            }

            await _auditRepository.SaveAsync(new AuthenticationEvent
            {
                UserId = userId,
                EventType = "AWID_CREATED",
                Details = $"Created AWID {awid.PublicAwid}"
            }, cancellationToken);

            return new AwidResponse
            {
                Success = true,
                PublicAwid = awid.PublicAwid,
                Alias = awid.AliasDisplay,
                Status = awid.Status.ToString().ToUpperInvariant(),
                Message = "AWID created successfully"
            };
        }

        return new AwidResponse { Success = false, ErrorCode = "AWID_GENERATION_COLLISION", Message = "Unable to generate unique AWID" };
    }

    private string GeneratePublicAwid()
    {
        Span<char> randomPart = stackalloc char[_policy.PublicAwidRandomLength];
        for (var i = 0; i < randomPart.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(PublicAwidAlphabet.Length);
            randomPart[i] = PublicAwidAlphabet[index];
        }

        return $"AW-{_policy.IssuedMarket}-{new string(randomPart)}";
    }

    private static AwidPrivacyMode ParsePrivacyMode(string privacyMode)
    {
        return privacyMode.ToUpperInvariant() switch
        {
            "STANDARD" => AwidPrivacyMode.Standard,
            "PROFESSIONAL" => AwidPrivacyMode.Professional,
            "CUSTOM" => AwidPrivacyMode.Custom,
            _ => AwidPrivacyMode.Private
        };
    }
}
