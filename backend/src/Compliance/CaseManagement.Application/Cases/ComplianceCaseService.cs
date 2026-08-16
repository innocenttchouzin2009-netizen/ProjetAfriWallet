using AfriWallet.Compliance.CaseManagement.Application.Abstractions;
using AfriWallet.Compliance.CaseManagement.Application.Policies;
using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Application.Cases;

public sealed class ComplianceCaseService
{
    private readonly IComplianceCaseRepository _repository;
    private readonly IComplianceCaseAuditStore _audit;
    private readonly IComplianceCaseClock _clock;
    private readonly CaseManagementPolicy _policy;

    public ComplianceCaseService(
        IComplianceCaseRepository repository,
        IComplianceCaseAuditStore audit,
        IComplianceCaseClock clock,
        CaseManagementPolicy policy)
    {
        _repository = repository;
        _audit = audit;
        _clock = clock;
        _policy = policy;
    }

    public async Task<ComplianceCaseResult> CreateAsync(CreateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var complianceCase = new ComplianceCase(Guid.NewGuid(), command.Awid, command.Title, command.Priority, _clock.UtcNow);
        await _repository.AddAsync(complianceCase, cancellationToken);
        await AuditAsync(complianceCase, "compliance.case.created", command.Actor, cancellationToken);
        return Map(complianceCase);
    }

    public async Task<ComplianceCaseResult> AddSourceAsync(AddCaseSourceCommand command, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(command.CaseId, cancellationToken);
        item.LinkSource(command.SourceType, command.SourceId, command.Summary, _policy.ResolvePriority(command.SourceType), _clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.source-linked", command.Actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> AssignAsync(AssignCaseCommand command, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(command.CaseId, cancellationToken);
        item.Assign(command.Assignee, command.Actor, _clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.assigned", command.Actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> StartReviewAsync(Guid caseId, string actor, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(caseId, cancellationToken);
        item.StartReview(_clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.review-started", actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> AddNoteAsync(AddCaseNoteCommand command, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(command.CaseId, cancellationToken);
        item.AddNote(command.Actor, command.Content, _clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.note-added", command.Actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> EscalateAsync(EscalateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(command.CaseId, cancellationToken);
        item.Escalate(command.Priority, _clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.escalated", command.Actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> ResolveAsync(ResolveCaseCommand command, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(command.CaseId, cancellationToken);
        item.Resolve(command.Decision, _clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.resolved", command.Actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> CloseAsync(Guid caseId, string actor, CancellationToken cancellationToken = default)
    {
        var item = await RequireAsync(caseId, cancellationToken);
        item.Close(_clock.UtcNow);
        await SaveAndAuditAsync(item, "compliance.case.closed", actor, cancellationToken);
        return Map(item);
    }

    public async Task<ComplianceCaseResult> GetAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        Map(await RequireAsync(caseId, cancellationToken));

    public async Task<IReadOnlyCollection<ComplianceCaseResult>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default) =>
        (await _repository.GetByAwidAsync(awid, cancellationToken)).Select(Map).ToArray();

    private async Task<ComplianceCase> RequireAsync(Guid caseId, CancellationToken cancellationToken) =>
        await _repository.GetAsync(caseId, cancellationToken) ?? throw new KeyNotFoundException("Compliance case not found.");

    private async Task SaveAndAuditAsync(ComplianceCase item, string eventType, string actor, CancellationToken cancellationToken)
    {
        await _repository.SaveAsync(item, cancellationToken);
        await AuditAsync(item, eventType, actor, cancellationToken);
    }

    private async Task AuditAsync(ComplianceCase item, string eventType, string actor, CancellationToken cancellationToken)
    {
        await _audit.AppendAsync(new ComplianceCaseAuditEvent(
            Guid.NewGuid(), item.CaseId, item.Awid, eventType, actor, _clock.UtcNow,
            new Dictionary<string, string>
            {
                ["status"] = item.Status.ToString(),
                ["priority"] = item.Priority.ToString(),
                ["decision"] = item.Decision.ToString()
            }), cancellationToken);
    }

    private static ComplianceCaseResult Map(ComplianceCase item) => new(
        item.CaseId, item.Awid, item.Title, item.Priority, item.Status, item.Decision,
        item.Assignment?.Assignee, item.Sources.Count, item.Notes.Count, item.CreatedAtUtc, item.UpdatedAtUtc);
}