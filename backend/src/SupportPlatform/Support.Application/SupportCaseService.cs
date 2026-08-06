using System.Text.RegularExpressions;
using Support.Contracts;
using Support.Domain;
using Support.Infrastructure;

namespace Support.Application;

public sealed class SupportCaseService
{
    private static readonly HashSet<(SupportCaseStatus From, SupportCaseStatus To)> AllowedTransitions = new()
    {
        (SupportCaseStatus.Open, SupportCaseStatus.Assigned),
        (SupportCaseStatus.Assigned, SupportCaseStatus.InProgress),
        (SupportCaseStatus.InProgress, SupportCaseStatus.WaitingForCustomer),
        (SupportCaseStatus.InProgress, SupportCaseStatus.WaitingForPartner),
        (SupportCaseStatus.InProgress, SupportCaseStatus.Escalated),
        (SupportCaseStatus.InProgress, SupportCaseStatus.Resolved),
        (SupportCaseStatus.Escalated, SupportCaseStatus.InProgress),
        (SupportCaseStatus.WaitingForCustomer, SupportCaseStatus.InProgress),
        (SupportCaseStatus.WaitingForPartner, SupportCaseStatus.InProgress),
        (SupportCaseStatus.Resolved, SupportCaseStatus.Closed),
        (SupportCaseStatus.Closed, SupportCaseStatus.Reopened),
        (SupportCaseStatus.Reopened, SupportCaseStatus.InProgress)
    };

    private static readonly HashSet<string> AllowedAttachmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "text/plain"
    };

    private const long MaxAttachmentSizeBytes = 5 * 1024 * 1024;

    private readonly InMemorySupportStore _store;
    private readonly AssignmentService _assignmentService;
    private readonly SlaService _slaService;
    private readonly EscalationService _escalationService;
    private readonly SupportTimelineService _timelineService;
    private readonly SupportNotificationService _notificationService;
    private readonly SupportSearchService _searchService;

    public SupportCaseService(
        InMemorySupportStore store,
        AssignmentService assignmentService,
        SlaService slaService,
        EscalationService escalationService,
        SupportTimelineService timelineService,
        SupportNotificationService notificationService,
        SupportSearchService searchService)
    {
        _store = store;
        _assignmentService = assignmentService;
        _slaService = slaService;
        _escalationService = escalationService;
        _timelineService = timelineService;
        _notificationService = notificationService;
        _searchService = searchService;
    }

    public SupportCaseResponse CreateCase(CreateSupportCaseRequest request, DateTimeOffset? nowOverrideUtc = null)
    {
        var nowUtc = nowOverrideUtc ?? DateTimeOffset.UtcNow;
        var supportCase = new SupportCase
        {
            CaseReference = BuildCaseReference(),
            RequesterAwidId = request.RequesterAwidId,
            MerchantId = request.MerchantId,
            DeveloperApplicationId = request.DeveloperApplicationId,
            Category = ParsingExtensions.ParseCategory(request.Category),
            Subcategory = request.Subcategory,
            Subject = request.Subject,
            Description = MaskSensitive(request.Description),
            Priority = ParsingExtensions.ParsePriority(request.Priority),
            Status = SupportCaseStatus.Open,
            Channel = ParsingExtensions.ParseChannel(request.Channel),
            RelatedTransactionId = request.RelatedTransactionId,
            RelatedWalletId = request.RelatedWalletId,
            RelatedCardId = request.RelatedCardId,
            RelatedSettlementId = request.RelatedSettlementId,
            RelatedComplianceCaseId = request.RelatedComplianceCaseId,
            SlaPolicyId = "default-v1",
            OpenedAtUtc = request.OpenedAtUtcOverride ?? nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        supportCase.Sla = _slaService.BuildSla(supportCase.SlaPolicyId, supportCase.Priority);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseCreated, "system", "Support case created", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseCreated);

        var assignment = _assignmentService.AssignAutomatically(supportCase, nowUtc);
        TransitionStatus(supportCase, SupportCaseStatus.Assigned, "system", "Case auto-assigned", nowUtc);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseAssigned, "system", $"Assigned to {assignment.Team}", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseAssigned);

        _store.Cases.Add(supportCase);
        IncrementMetric("afw_support_cases_created_total");
        IncrementMetric("afw_support_cases_open_total");
        _notificationService.SendCaseNotification(supportCase, "CASE_CREATED");

        return Map(supportCase);
    }

    public IReadOnlyList<SupportCaseResponse> ListCases(SupportCaseQuery query)
    {
        return _searchService.Search(_store.Cases, query).Select(Map).ToList();
    }

    public SupportCaseResponse GetCase(Guid caseId)
    {
        return Map(GetRequiredCase(caseId));
    }

    public SupportCaseResponse UpdateCase(Guid caseId, UpdateSupportCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            supportCase.Subject = request.Subject;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            supportCase.Description = MaskSensitive(request.Description);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            supportCase.Priority = ParsingExtensions.ParsePriority(request.Priority);
            supportCase.Sla = _slaService.BuildSla(supportCase.SlaPolicyId, supportCase.Priority);
            _timelineService.Add(supportCase, SupportTimelineEventType.PriorityChanged, "agent", "Priority changed", nowUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var targetStatus = ParsingExtensions.ParseStatus(request.Status);
            TransitionStatus(supportCase, targetStatus, "agent", "Status updated", nowUtc);
        }

        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;
        return Map(supportCase);
    }

    public SupportCaseResponse AssignCase(Guid caseId, AssignCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        var isReassignment = !string.IsNullOrWhiteSpace(supportCase.AssignedTeam);

        var assignment = _assignmentService.AssignManually(supportCase, request.AssignedTeam, request.AssignedAgentId, nowUtc);
        if (supportCase.Status is SupportCaseStatus.Open or SupportCaseStatus.Reopened)
        {
            TransitionStatus(supportCase, SupportCaseStatus.Assigned, request.AssignedAgentId ?? "agent", "Case manually assigned", nowUtc);
        }

        _timelineService.Add(supportCase, SupportTimelineEventType.CaseAssigned, request.AssignedAgentId ?? "agent", $"Assigned to {assignment.Team}", nowUtc);
        supportCase.AuditEvents.Add(isReassignment ? SupportAuditEvent.SupportCaseReassigned : SupportAuditEvent.SupportCaseAssigned);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;

        _notificationService.SendCaseNotification(supportCase, isReassignment ? "CASE_REASSIGNED" : "CASE_ASSIGNED");
        return Map(supportCase);
    }

    public SupportMessageResponse AddMessage(Guid caseId, AddSupportMessageRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        if (supportCase.Status == SupportCaseStatus.Assigned)
        {
            TransitionStatus(supportCase, SupportCaseStatus.InProgress, request.AuthorId, "Case in progress", nowUtc);
        }

        if (!request.IsFromCustomer && !supportCase.FirstResponseAtUtc.HasValue)
        {
            supportCase.FirstResponseAtUtc = nowUtc;
            supportCase.Sla.FirstResponseAtUtc = nowUtc;
            _store.Metrics["afw_support_first_response_duration_ms"] = (long)(nowUtc - supportCase.OpenedAtUtc).TotalMilliseconds;
        }

        var message = new SupportMessage
        {
            CaseId = supportCase.CaseId,
            AuthorId = request.AuthorId,
            IsFromCustomer = request.IsFromCustomer,
            Body = MaskSensitive(request.Body),
            CreatedAtUtc = nowUtc
        };

        supportCase.Messages.Add(message);
        _timelineService.Add(supportCase, SupportTimelineEventType.MessageAdded, request.AuthorId, "Support message added", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportMessageAdded);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;

        _notificationService.SendCaseNotification(supportCase, request.IsFromCustomer ? "CUSTOMER_MESSAGE" : "AGENT_MESSAGE");
        EvaluateSlaInternal(supportCase, nowUtc);

        return new SupportMessageResponse
        {
            MessageId = message.MessageId,
            AuthorId = message.AuthorId,
            IsFromCustomer = message.IsFromCustomer,
            Body = message.Body,
            CreatedAtUtc = message.CreatedAtUtc
        };
    }

    public IReadOnlyList<SupportMessageResponse> GetCustomerVisibleMessages(Guid caseId)
    {
        return GetRequiredCase(caseId)
            .Messages
            .Select(x => new SupportMessageResponse
            {
                MessageId = x.MessageId,
                AuthorId = x.AuthorId,
                IsFromCustomer = x.IsFromCustomer,
                Body = x.Body,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }

    public SupportNoteResponse AddInternalNote(Guid caseId, AddSupportNoteRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        var note = new SupportNote
        {
            CaseId = supportCase.CaseId,
            AuthorAgentId = request.AuthorAgentId,
            Content = MaskSensitive(request.Content),
            CreatedAtUtc = nowUtc
        };

        supportCase.Notes.Add(note);
        _timelineService.Add(supportCase, SupportTimelineEventType.InternalNoteAdded, request.AuthorAgentId, "Internal note added", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportInternalNoteAdded);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;

        return new SupportNoteResponse
        {
            NoteId = note.NoteId,
            AuthorAgentId = note.AuthorAgentId,
            Content = note.Content,
            CreatedAtUtc = note.CreatedAtUtc
        };
    }

    public SupportAttachmentResponse AddAttachment(Guid caseId, AddSupportAttachmentRequest request)
    {
        if (!AllowedAttachmentTypes.Contains(request.ContentType))
        {
            throw new InvalidOperationException("Attachment content type is not allowed.");
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > MaxAttachmentSizeBytes)
        {
            throw new InvalidOperationException("Attachment size is invalid.");
        }

        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        var attachment = new SupportAttachment
        {
            CaseId = supportCase.CaseId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            IsInternalOnly = request.IsInternalOnly,
            UploadedBy = request.UploadedBy,
            CreatedAtUtc = nowUtc
        };

        supportCase.Attachments.Add(attachment);
        _timelineService.Add(supportCase, SupportTimelineEventType.AttachmentAdded, request.UploadedBy, "Attachment added", nowUtc);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;

        return new SupportAttachmentResponse
        {
            AttachmentId = attachment.AttachmentId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            IsInternalOnly = attachment.IsInternalOnly,
            CreatedAtUtc = attachment.CreatedAtUtc
        };
    }

    public SupportCaseResponse EscalateCase(Guid caseId, EscalateCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        if (supportCase.Status != SupportCaseStatus.InProgress)
        {
            throw new InvalidOperationException("Case must be in progress before escalation.");
        }

        _escalationService.Escalate(supportCase, request.Level, request.Reason, nowUtc);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseEscalated, "agent", $"Escalated to {request.Level}", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseEscalated);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;
        IncrementMetric("afw_support_cases_escalated_total");

        _notificationService.SendCaseNotification(supportCase, "CASE_ESCALATED");
        return Map(supportCase);
    }

    public SupportCaseResponse ResolveCase(Guid caseId, ResolveCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;

        if (supportCase.Status != SupportCaseStatus.InProgress && supportCase.Status != SupportCaseStatus.Escalated)
        {
            throw new InvalidOperationException("Case must be in progress or escalated before resolution.");
        }

        if (supportCase.Status == SupportCaseStatus.Escalated)
        {
            TransitionStatus(supportCase, SupportCaseStatus.InProgress, request.ResolvedByAgentId, "Returned from escalation", nowUtc);
        }

        TransitionStatus(supportCase, SupportCaseStatus.Resolved, request.ResolvedByAgentId, request.ResolutionSummary, nowUtc);
        supportCase.ResolvedAtUtc = nowUtc;
        supportCase.Sla.ResolvedAtUtc = nowUtc;
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseResolved);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseResolved, request.ResolvedByAgentId, "Case resolved", nowUtc);
        _store.Metrics["afw_support_resolution_duration_ms"] = (long)(nowUtc - supportCase.OpenedAtUtc).TotalMilliseconds;
        IncrementMetric("afw_support_cases_resolved_total");
        DecrementMetric("afw_support_cases_open_total");

        _notificationService.SendCaseNotification(supportCase, "CASE_RESOLVED");
        return Map(supportCase);
    }

    public SupportCaseResponse CloseCase(Guid caseId, CloseCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        if (supportCase.Status != SupportCaseStatus.Resolved)
        {
            throw new InvalidOperationException("Case must be resolved before close.");
        }

        TransitionStatus(supportCase, SupportCaseStatus.Closed, request.ClosedByAgentId, request.Reason, nowUtc);
        supportCase.ClosedAtUtc = nowUtc;
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseClosed);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseClosed, request.ClosedByAgentId, "Case closed", nowUtc);

        _notificationService.SendCaseNotification(supportCase, "CASE_CLOSED");
        return Map(supportCase);
    }

    public SupportCaseResponse ReopenCase(Guid caseId, ReopenCaseRequest request)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = DateTimeOffset.UtcNow;
        if (supportCase.Status != SupportCaseStatus.Closed)
        {
            throw new InvalidOperationException("Case must be closed before reopen.");
        }

        TransitionStatus(supportCase, SupportCaseStatus.Reopened, request.ReopenedBy, request.Reason, nowUtc);
        _timelineService.Add(supportCase, SupportTimelineEventType.CaseReopened, request.ReopenedBy, "Case reopened", nowUtc);
        supportCase.AuditEvents.Add(SupportAuditEvent.SupportCaseReopened);
        TransitionStatus(supportCase, SupportCaseStatus.InProgress, request.ReopenedBy, "Case reactivated", nowUtc);
        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;
        IncrementMetric("afw_support_cases_open_total");

        _notificationService.SendCaseNotification(supportCase, "CASE_REOPENED");
        return Map(supportCase);
    }

    public IReadOnlyList<SupportTimelineEntryResponse> GetTimeline(Guid caseId)
    {
        return GetRequiredCase(caseId).Timeline
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new SupportTimelineEntryResponse
            {
                EventType = x.EventType,
                ActorId = x.ActorId,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }

    public SupportSlaResponse GetSla(Guid caseId, DateTimeOffset? nowOverrideUtc = null)
    {
        var supportCase = GetRequiredCase(caseId);
        var nowUtc = nowOverrideUtc ?? DateTimeOffset.UtcNow;
        return EvaluateSlaInternal(supportCase, nowUtc);
    }

    public (int ExternalNotificationsSent, int InternalAlertsSent) GetNotificationStats()
    {
        return (_notificationService.ExternalNotificationsSent, _notificationService.InternalAlertsSent);
    }

    private SupportSlaResponse EvaluateSlaInternal(SupportCase supportCase, DateTimeOffset nowUtc)
    {
        var evaluation = _slaService.Evaluate(supportCase, nowUtc);
        if (evaluation.WarningTriggered)
        {
            _timelineService.Add(supportCase, SupportTimelineEventType.SlaWarning, "system", "SLA warning threshold reached", nowUtc);
            supportCase.AuditEvents.Add(SupportAuditEvent.SupportSlaWarning);
            _notificationService.SendInternalSlaAlert(supportCase, "SLA_WARNING");
        }

        if (evaluation.Breached)
        {
            _timelineService.Add(supportCase, SupportTimelineEventType.SlaBreached, "system", "SLA breached", nowUtc);
            supportCase.AuditEvents.Add(SupportAuditEvent.SupportSlaBreached);
            IncrementMetric("afw_support_sla_breaches_total");
            _notificationService.SendInternalSlaAlert(supportCase, "SLA_BREACHED");
        }

        return new SupportSlaResponse
        {
            PolicyId = supportCase.Sla.PolicyId,
            FirstResponseTargetMinutes = (long)supportCase.Sla.FirstResponseTarget.TotalMinutes,
            ResolutionTargetMinutes = (long)supportCase.Sla.ResolutionTarget.TotalMinutes,
            WarningTriggered = evaluation.WarningTriggered,
            Breached = supportCase.Sla.Violations.Count > 0,
            Violations = supportCase.Sla.Violations.ToList()
        };
    }

    private void TransitionStatus(SupportCase supportCase, SupportCaseStatus target, string actorId, string reason, DateTimeOffset nowUtc)
    {
        if (supportCase.Status == target)
        {
            return;
        }

        if (!AllowedTransitions.Contains((supportCase.Status, target)))
        {
            throw new InvalidOperationException($"Invalid support case transition: {supportCase.Status} -> {target}");
        }

        var source = supportCase.Status;
        supportCase.Status = target;

        if (target == SupportCaseStatus.WaitingForCustomer)
        {
            _slaService.Pause(supportCase, nowUtc);
        }
        else if (source == SupportCaseStatus.WaitingForCustomer)
        {
            _slaService.Resume(supportCase, nowUtc);
        }

        _timelineService.Add(
            supportCase,
            SupportTimelineEventType.StatusChanged,
            actorId,
            $"Status changed from {source.ToWire()} to {target.ToWire()} ({reason})",
            nowUtc);

        supportCase.UpdatedAtUtc = nowUtc;
        supportCase.Version += 1;
    }

    private SupportCase GetRequiredCase(Guid caseId)
    {
        return _store.Cases.Single(x => x.CaseId == caseId);
    }

    private string BuildCaseReference()
    {
        return $"SUP-{DateTimeOffset.UtcNow:yyyyMMdd}-{_store.Cases.Count + 1:D5}";
    }

    private static string MaskSensitive(string value)
    {
        var masked = Regex.Replace(value, @"\b\d{12,19}\b", "[REDACTED_NUMBER]");
        masked = Regex.Replace(masked, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", "[REDACTED_EMAIL]", RegexOptions.IgnoreCase);
        return masked;
    }

    private void IncrementMetric(string key)
    {
        _store.Metrics[key] += 1;
    }

    private void DecrementMetric(string key)
    {
        _store.Metrics[key] = Math.Max(0, _store.Metrics[key] - 1);
    }

    private SupportCaseResponse Map(SupportCase supportCase)
    {
        return new SupportCaseResponse
        {
            CaseId = supportCase.CaseId,
            CaseReference = supportCase.CaseReference,
            RequesterAwidId = supportCase.RequesterAwidId,
            Category = supportCase.Category.ToWire(),
            Subcategory = supportCase.Subcategory,
            Subject = supportCase.Subject,
            Description = supportCase.Description,
            Priority = supportCase.Priority.ToString().ToUpperInvariant(),
            Status = supportCase.Status.ToWire(),
            Channel = supportCase.Channel.ToWire(),
            AssignedTeam = supportCase.AssignedTeam,
            AssignedAgentId = supportCase.AssignedAgentId,
            RelatedComplianceCaseId = supportCase.RelatedComplianceCaseId,
            OpenedAtUtc = supportCase.OpenedAtUtc,
            FirstResponseAtUtc = supportCase.FirstResponseAtUtc,
            ResolvedAtUtc = supportCase.ResolvedAtUtc,
            ClosedAtUtc = supportCase.ClosedAtUtc,
            Version = supportCase.Version,
            Sla = new SupportSlaResponse
            {
                PolicyId = supportCase.Sla.PolicyId,
                FirstResponseTargetMinutes = (long)supportCase.Sla.FirstResponseTarget.TotalMinutes,
                ResolutionTargetMinutes = (long)supportCase.Sla.ResolutionTarget.TotalMinutes,
                WarningTriggered = false,
                Breached = supportCase.Sla.Violations.Count > 0,
                Violations = supportCase.Sla.Violations.ToList()
            },
            Timeline = supportCase.Timeline
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new SupportTimelineEntryResponse
                {
                    EventType = x.EventType,
                    ActorId = x.ActorId,
                    Description = x.Description,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList(),
            AuditEvents = supportCase.AuditEvents.ToList(),
            Telemetry = new Dictionary<string, long>(_store.Metrics, StringComparer.OrdinalIgnoreCase)
        };
    }
}
