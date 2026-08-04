namespace MobileMoney.Production.Audit;

public sealed class AuditService : IAuditService
{
    private readonly AuditRepository _repository;

    public AuditService(AuditRepository repository)
    {
        _repository = repository;
    }

    public AuditRecord Record(AuditRecord record)
    {
        var previous = _repository.All().LastOrDefault();
        record.PreviousAuditHash = previous?.CurrentAuditHash;
        record.CurrentAuditHash = AuditHashCalculator.Calculate(record.PreviousAuditHash, record.AuditId, record.Action, record.Result, record.TimestampUtc);
        record.PhoneNumber = SanitizePhone(record.PhoneNumber);
        return _repository.Save(record);
    }

    public AuditRecord Record(string action, AuditResult result, AuditCategory category, AuditContext context)
    {
        var actionValue = Enum.Parse<AuditAction>(action, true);
        return Record(new AuditRecord
        {
            Action = actionValue,
            Category = category,
            Result = result,
            CorrelationId = context.CorrelationId,
            TraceId = context.TraceId,
            TransactionId = context.TransactionId,
            ProviderReference = context.ProviderReference,
            AwidId = context.AwidId,
            WalletId = context.WalletId,
            ProviderCode = context.ProviderCode,
            OperationType = context.OperationType,
            ActorType = context.ActorType,
            ActorId = context.ActorId,
            Environment = context.Environment,
            IpAddress = context.IpAddress,
            DeviceId = context.DeviceId,
            PhoneNumber = context.PhoneNumber,
            DurationMs = context.DurationMs,
            PreviousAuditHash = context.PreviousAuditHash
        });
    }

    public IReadOnlyList<AuditRecord> Search(AuditSearchCriteria criteria) => new AuditSearchService(_repository).Search(criteria);

    public AuditRecord? GetById(string auditId) => _repository.GetById(auditId);

    public IReadOnlyList<AuditRecord> Export(AuditExportFilter filter) => _repository.All().Where(x =>
        (filter.From is null || x.TimestampUtc >= filter.From.Value) &&
        (filter.To is null || x.TimestampUtc <= filter.To.Value) &&
        (filter.ProviderCode is null || x.ProviderCode == filter.ProviderCode) &&
        (filter.Result is null || x.Result == filter.Result) &&
        (filter.OperationType is null || x.OperationType == filter.OperationType)).ToList();

    public bool VerifyChain(string auditId)
    {
        var records = _repository.All().Where(x => x.AuditId == auditId || x.TransactionId == _repository.GetById(auditId)?.TransactionId).OrderBy(x => x.TimestampUtc).ToList();
        return AuditChainValidator.IsChainValid(records);
    }

    private static string? SanitizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        return $"{new string('*', value.Length - 4)}{value[^4..]}";
    }
}

public sealed class AuditExportFilter
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? ProviderCode { get; init; }
    public AuditResult? Result { get; init; }
    public string? OperationType { get; init; }
}
