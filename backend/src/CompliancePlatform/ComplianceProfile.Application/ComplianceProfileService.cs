using AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;
using ComplianceProfileDomain = AfriWallet.CompliancePlatform.ComplianceProfile.Domain.ComplianceProfile;

namespace AfriWallet.CompliancePlatform.ComplianceProfile.Application;

public sealed class ComplianceProfileService
{
    private readonly IComplianceProfileRepository _repository;
    private readonly IComplianceAuditSink _audit;

    public ComplianceProfileService(IComplianceProfileRepository repository, IComplianceAuditSink audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task<ComplianceProfileView> CreateAsync(CreateComplianceProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = ComplianceProfileDomain.Create(request.CustomerId, request.ProfileType, request.SourceChannel, request.RiskLevel);
        await _repository.AddAsync(profile, cancellationToken);
        await _audit.WriteAsync(profile.ProfileId.ToString(), "PROFILE_CREATED", $"customer={profile.CustomerId};type={profile.ProfileType}", cancellationToken);
        return Map(profile);
    }

    public async Task<ComplianceProfileView?> GetAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetAsync(profileId, cancellationToken);
        return profile is null ? null : Map(profile);
    }

    public async Task<IReadOnlyCollection<ComplianceProfileView>> ListByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        var items = await _repository.ListByCustomerAsync(customerId, cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<ComplianceProfileView> AddDocumentAsync(AddDocumentRequest request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetAsync(request.ProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Profile not found.");

        profile.AddDocument(request.DocumentType, request.Reference, request.CountryCode, request.ProviderName);
        await _audit.WriteAsync(profile.ProfileId.ToString(), "DOCUMENT_ADDED", request.DocumentType, cancellationToken);

        return Map(profile);
    }

    public async Task<ComplianceProfileView> ReviewAsync(ReviewComplianceProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetAsync(request.ProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Profile not found.");

        if (request.IsApproved)
        {
            profile.Approve(request.Reviewer, request.DecisionReason);
        }
        else
        {
            profile.Reject(request.Reviewer, request.DecisionReason);
        }

        await _audit.WriteAsync(profile.ProfileId.ToString(), request.IsApproved ? "PROFILE_APPROVED" : "PROFILE_REJECTED", request.DecisionReason, cancellationToken);

        return Map(profile);
    }

    public async Task<ComplianceProfileView> SuspendAsync(Guid profileId, string reviewer, string reason, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Profile not found.");

        profile.Suspend(reviewer, reason);
        await _audit.WriteAsync(profile.ProfileId.ToString(), "PROFILE_SUSPENDED", reason, cancellationToken);

        return Map(profile);
    }

    private static ComplianceProfileView Map(ComplianceProfileDomain profile)
    {
        return new ComplianceProfileView(
            profile.ProfileId,
            profile.CustomerId,
            profile.ProfileType,
            profile.SourceChannel,
            profile.RiskLevel,
            profile.Status.ToString(),
            profile.ReviewStage,
            profile.LastDecisionReason,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.Documents.Select(x => new ComplianceProfileDocumentView(
                x.DocumentId,
                x.DocumentType,
                x.Reference,
                x.CountryCode,
                x.ProviderName,
                x.Status,
                x.CreatedAt)).ToArray(),
            profile.AuditTrail.ToArray());
    }
}
